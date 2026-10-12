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
/// </summary>
public class VentaService
{
    private readonly ProductoRepository _productoRepository = new();
    private readonly VentaRepository _ventaRepository = new();
    private readonly CajaRepository _cajaRepository = new();

    private readonly List<DetalleVenta> _carrito = new();

    public IReadOnlyList<DetalleVenta> Carrito => _carrito;

    public decimal Total => _carrito.Sum(d => d.Subtotal);

    public void IniciarVenta()
    {
        _carrito.Clear();
    }

    public (bool Exitoso, string Mensaje) AgregarProducto(int idProducto, int cantidad)
    {
        var validacionCantidad = Validaciones.ValidarCantidad(cantidad);
        if (!validacionCantidad.EsValido)
        {
            return (false, validacionCantidad.Mensaje);
        }

        var producto = _productoRepository.ObtenerPorId(idProducto);
        if (producto is null || !producto.Estado)
        {
            return (false, "El producto seleccionado no está disponible.");
        }

        var cantidadYaEnCarrito = _carrito
            .Where(d => d.IdProducto == idProducto)
            .Sum(d => d.Cantidad);

        var validacionExistencia = Validaciones.ValidarExistenciaSuficiente(
            producto.Existencia, cantidadYaEnCarrito + cantidad);
        if (!validacionExistencia.EsValido)
        {
            return (false, validacionExistencia.Mensaje);
        }

        var lineaExistente = _carrito.FirstOrDefault(d => d.IdProducto == idProducto);
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
                PrecioUnitario = producto.PrecioVenta
            });
        }

        return (true, string.Empty);
    }

    public void EliminarProducto(int idProducto)
    {
        _carrito.RemoveAll(d => d.IdProducto == idProducto);
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

        var total = Total;
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
                MetodoPago = metodoPago,
                MontoRecibido = montoRecibido,
                Cambio = cambio,
                IdCaja = caja.IdCaja,
                Detalles = new List<DetalleVenta>(_carrito)
            };

            venta.IdVenta = _ventaRepository.RegistrarVentaCompleta(venta);
            _carrito.Clear();
            return (true, "Venta registrada correctamente.", venta);
        }
        catch (InvalidOperationException ex)
        {
            // Existencia insuficiente detectada dentro de la transacción (RN-05).
            return (false, $"{ex.Message} Verifique las existencias; la venta no se registró.", null);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo registrar la venta. No se guardó ningún cambio.\nDetalle: {ex.Message}", null);
        }
    }

    public static decimal CalcularCambio(decimal total, decimal montoRecibido)
    {
        return montoRecibido - total;
    }

    public bool HayCajaAbierta()
    {
        return _cajaRepository.ObtenerCajaAbierta() is not null;
    }
}
