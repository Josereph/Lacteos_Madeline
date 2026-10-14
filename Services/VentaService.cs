using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Orquesta el flujo de una venta: arma el carrito en memoria, valida
/// cada línea contra el inventario disponible y, al confirmar, registra
/// la venta completa (cabecera + detalle) descontando existencias de
/// forma atómica. Sigue el flujo descrito en la sección 8 de los
/// lineamientos: iniciar venta → agregar productos → total → confirmar → ticket.
/// También anula ventas ya registradas.
/// </summary>
public class VentaService
{
    private readonly ProductoRepository _productoRepository = new();
    private readonly VentaRepository _ventaRepository = new();
    private readonly CajaRepository _cajaRepository = new();
    private readonly ClienteRepository _clienteRepository = new();

    private readonly List<DetalleVenta> _carrito = new();

    public IReadOnlyList<DetalleVenta> Carrito => _carrito;

    /// <summary>Suma de las líneas, antes del descuento.</summary>
    public decimal Subtotal => _carrito.Sum(d => d.Subtotal);

    public decimal Descuento { get; private set; }

    /// <summary>Administrador que autorizó el descuento (si quien vende es cajero).</summary>
    public int? IdUsuarioDescuento { get; private set; }

    /// <summary>Lo que paga el cliente.</summary>
    public decimal Total => Math.Max(0m, Subtotal - Descuento);

    public Cliente? Cliente { get; private set; }

    public void IniciarVenta()
    {
        _carrito.Clear();
        Descuento = 0m;
        IdUsuarioDescuento = null;
        Cliente = null;
    }

    public (bool Exitoso, string Mensaje) AgregarProducto(int idProducto, decimal cantidad)
    {
        var producto = _productoRepository.ObtenerPorId(idProducto);
        if (producto is null || !producto.Estado)
        {
            return (false, "El producto seleccionado no está disponible.");
        }

        return Agregar(producto, cantidad);
    }

    /// <summary>
    /// Agrega el producto con ese código (lector de código de barras).
    /// Devuelve false si no hay un producto activo con ese código.
    /// </summary>
    public (bool Exitoso, string Mensaje) AgregarPorCodigo(string codigo, decimal cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return (false, "Escriba o escanee un código.");
        }

        var producto = _productoRepository.ObtenerPorCodigo(codigo);
        if (producto is null || !producto.Estado)
        {
            return (false, $"No hay un producto activo con el código \"{codigo.Trim()}\".");
        }

        return Agregar(producto, cantidad);
    }

    private (bool Exitoso, string Mensaje) Agregar(Producto producto, decimal cantidad)
    {
        var validacionCantidad = Validaciones.ValidarCantidad(cantidad, producto.Unidad);
        if (!validacionCantidad.EsValido)
        {
            return (false, validacionCantidad.Mensaje);
        }

        var cantidadYaEnCarrito = _carrito
            .Where(d => d.IdProducto == producto.IdProducto)
            .Sum(d => d.Cantidad);

        var validacionExistencia = Validaciones.ValidarExistenciaSuficiente(
            producto.Existencia, cantidadYaEnCarrito + cantidad);
        if (!validacionExistencia.EsValido)
        {
            return (false, $"Existencia insuficiente de \"{producto.Nombre}\": hay {Cantidades.Formatear(producto.Existencia, producto.Unidad)}.");
        }

        var lineaExistente = _carrito.FirstOrDefault(d => d.IdProducto == producto.IdProducto);
        if (lineaExistente is not null)
        {
            lineaExistente.Cantidad += cantidad;
        }
        else
        {
            _carrito.Add(new DetalleVenta
            {
                IdProducto = producto.IdProducto,
                NombreProducto = producto.Nombre,
                Cantidad = cantidad,
                Unidad = producto.Unidad,
                PrecioUnitario = producto.PrecioVenta
            });
        }

        AjustarDescuentoAlSubtotal();
        return (true, $"{Cantidades.Formatear(cantidad, producto.Unidad)} × {producto.Nombre} agregado.");
    }

    public void EliminarProducto(int idProducto)
    {
        _carrito.RemoveAll(d => d.IdProducto == idProducto);
        AjustarDescuentoAlSubtotal();
    }

    /// <summary>
    /// Descuento en dinero sobre toda la venta. Si quien vende es cajero,
    /// <paramref name="idAutoriza"/> debe ser el administrador que lo autorizó.
    /// </summary>
    public (bool Exitoso, string Mensaje) AplicarDescuento(decimal monto, int? idAutoriza = null)
    {
        if (_carrito.Count == 0)
        {
            return (false, "Agregue productos antes de aplicar un descuento.");
        }

        if (monto < 0 || Dinero.TieneMasDeDosDecimales(monto))
        {
            return (false, "El descuento debe ser un monto positivo con hasta dos decimales.");
        }

        if (monto > Subtotal)
        {
            return (false, $"El descuento no puede ser mayor que el subtotal ({Subtotal:C2}).");
        }

        if (monto > 0 && !Sesion.PuedeHacerAccionDeAdministrador(idAutoriza))
        {
            return (false, "Los descuentos requieren la autorización de un administrador.");
        }

        Descuento = monto;
        IdUsuarioDescuento = monto > 0 ? idAutoriza ?? Sesion.IdUsuario : null;
        return (true, monto == 0 ? "Descuento quitado." : $"Descuento de {monto:C2} aplicado.");
    }

    /// <summary>Descuento como porcentaje del subtotal (redondeado a centavos).</summary>
    public (bool Exitoso, string Mensaje) AplicarDescuentoPorcentaje(decimal porcentaje, int? idAutoriza = null)
    {
        if (porcentaje < 0 || porcentaje > 100)
        {
            return (false, "El porcentaje debe estar entre 0 y 100.");
        }

        return AplicarDescuento(Dinero.Redondear(Subtotal * porcentaje / 100m), idAutoriza);
    }

    /// <summary>Si se quitan productos, el descuento no puede quedar mayor que el subtotal.</summary>
    private void AjustarDescuentoAlSubtotal()
    {
        if (_carrito.Count == 0 || Descuento > Subtotal)
        {
            Descuento = 0m;
            IdUsuarioDescuento = null;
        }
    }

    /// <summary>Asocia la venta a un cliente, o la deja como consumidor final (null).</summary>
    public (bool Exitoso, string Mensaje) SeleccionarCliente(int? idCliente)
    {
        if (idCliente is null)
        {
            Cliente = null;
            return (true, string.Empty);
        }

        var cliente = _clienteRepository.ObtenerPorId(idCliente.Value);
        if (cliente is null || !cliente.Estado)
        {
            return (false, "El cliente seleccionado no existe o está inactivo.");
        }

        Cliente = cliente;
        return (true, string.Empty);
    }

    /// <summary>
    /// Registra la venta. Para pagos en efectivo, <paramref name="montoRecibido"/>
    /// debe cubrir el total y el cambio se calcula automáticamente (RN-12).
    /// La venta queda asociada a la caja abierta; sin caja abierta no se vende,
    /// para que el efectivo de la jornada siempre cuadre en el cierre.
    /// </summary>
    public (bool Exitoso, string Mensaje, Venta? VentaRegistrada) ConfirmarVenta(string metodoPago, decimal? montoRecibido)
    {
        if (_carrito.Count == 0)
        {
            return (false, "Debe agregar al menos un producto a la venta.", null);
        }

        if (!Venta.MetodosPago.Contains(metodoPago))
        {
            return (false, "Seleccione un método de pago válido.", null);
        }

        // Revalidación de cada línea justo antes de guardar: ninguna venta
        // con cantidades en cero o precios en cero/negativos (la base de
        // datos lo vuelve a impedir con TR_DetalleVentas_Valido).
        foreach (var linea in _carrito)
        {
            if (linea.Cantidad <= 0 || linea.PrecioUnitario <= 0)
            {
                return (false, $"La línea \"{linea.NombreProducto}\" tiene cantidad o precio inválido. Elimínela y agréguela de nuevo.", null);
            }
        }

        if (Descuento > Subtotal)
        {
            return (false, "El descuento es mayor que el subtotal. Revíselo antes de cobrar.", null);
        }

        var total = Dinero.Redondear(Total);
        decimal? cambio = null;

        if (metodoPago == Venta.PagoEfectivo)
        {
            var validacionPago = Validaciones.ValidarPagoEfectivo(total, montoRecibido);
            if (!validacionPago.EsValido)
            {
                return (false, validacionPago.Mensaje, null);
            }

            cambio = CalcularCambio(total, montoRecibido!.Value);
        }
        else
        {
            montoRecibido = null;
        }

        try
        {
            var caja = _cajaRepository.ObtenerCajaAbierta();
            if (caja is null)
            {
                return (false, "No hay una caja abierta. Abra la caja antes de registrar ventas.", null);
            }

            var venta = new Venta
            {
                FechaHora = DateTime.Now,
                Total = total,
                Descuento = Descuento,
                MetodoPago = metodoPago,
                MontoRecibido = montoRecibido,
                Cambio = cambio,
                IdCaja = caja.IdCaja,
                IdCliente = Cliente?.IdCliente,
                NombreCliente = Cliente?.Nombre,
                IdUsuario = Sesion.IdUsuario,
                NombreUsuario = Sesion.Actual?.Nombre,
                IdUsuarioDescuento = IdUsuarioDescuento,
                Detalles = new List<DetalleVenta>(_carrito)
            };

            venta.IdVenta = _ventaRepository.RegistrarVentaCompleta(venta);
            IniciarVenta();
            return (true, "Venta registrada correctamente.", venta);
        }
        catch (ReglaNegocioException ex)
        {
            // Existencia insuficiente detectada dentro de la transacción (RN-05).
            return (false, $"{ex.Message} Verifique las existencias; la venta no se registró.", null);
        }
        catch (Exception ex) when (ErroresBaseDatos.EsReglaDeIntegridad(ex))
        {
            LogHelper.Advertencia("Venta rechazada por regla de integridad", ex.Message);
            return (false, $"{ErroresBaseDatos.MensajeAmigable(ex)} La venta no se registró.", null);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Confirmar venta (total {total:F2}, {_carrito.Count} líneas, {metodoPago})", ex);
            return (false, $"No se pudo registrar la venta. No se guardó ningún cambio.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}", null);
        }
    }

    public static decimal CalcularCambio(decimal total, decimal montoRecibido)
    {
        return Dinero.Redondear(montoRecibido - total);
    }

    public bool HayCajaAbierta()
    {
        return _cajaRepository.ObtenerCajaAbierta() is not null;
    }

    // ── Anulación ───────────────────────────────────────────────────────

    /// <summary>
    /// Anula una venta: devuelve las existencias y la saca del arqueo (o,
    /// si es de una caja ya cerrada y se pagó en efectivo, registra la
    /// devolución como salida de la caja abierta). Reservado a
    /// administradores: un cajero necesita la autorización
    /// (<paramref name="idAutoriza"/>) de uno.
    /// </summary>
    public (bool Exitoso, string Mensaje) Anular(int idVenta, string? motivo, int? idAutoriza = null)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5)
        {
            return (false, "Indique el motivo de la anulación (al menos 5 caracteres).");
        }

        if (!Sesion.PuedeHacerAccionDeAdministrador(idAutoriza))
        {
            return (false, "Anular una venta requiere la autorización de un administrador.");
        }

        try
        {
            _ventaRepository.Anular(idVenta, motivo.Trim(), idAutoriza ?? Sesion.IdUsuario);
            LogHelper.Info("Anulación de venta", $"Venta #{idVenta} anulada. Motivo: {motivo.Trim()}");
            return (true, $"Venta #{idVenta} anulada. Las existencias se devolvieron al inventario.");
        }
        catch (ReglaNegocioException ex)
        {
            return (false, ex.Message);
        }
        catch (Exception ex) when (ErroresBaseDatos.EsReglaDeIntegridad(ex))
        {
            return (false, ErroresBaseDatos.MensajeAmigable(ex));
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Anular venta #{idVenta}", ex);
            return (false, $"No se pudo anular la venta. No se guardó ningún cambio.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    // ── Consultas de ventas registradas ─────────────────────────────────

    /// <summary>Venta con su detalle, o null si no existe.</summary>
    public Venta? ObtenerPorId(int idVenta)
    {
        return _ventaRepository.ObtenerPorId(idVenta);
    }

    /// <summary>Ventas del rango de fechas, más recientes primero.</summary>
    public List<Venta> ConsultarPorRango(DateTime? desde, DateTime? hasta)
    {
        return _ventaRepository.ObtenerPorRango(desde, hasta);
    }
}
