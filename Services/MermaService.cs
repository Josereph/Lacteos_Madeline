using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Registro de mermas: productos dados de baja por caducidad, daño u otro
/// motivo. Descuenta existencias automáticamente, pero no genera una venta
/// ni mueve dinero de caja, por lo que no altera los reportes de ventas ni
/// el arqueo. La pérdida se valora con el precio de compra del producto.
/// </summary>
public class MermaService
{
    private readonly ProductoRepository _productoRepository = new();
    private readonly MermaRepository _mermaRepository = new();

    public (bool Exitoso, string Mensaje, Merma? MermaRegistrada) Registrar(
        int idProducto, decimal cantidad, string motivo, string? observacion)
    {
        if (cantidad <= 0)
        {
            return (false, "La cantidad debe ser mayor que cero.", null);
        }

        if (!Merma.Motivos.Contains(motivo))
        {
            return (false, "Seleccione un motivo válido (Caducidad, Daño u Otro).", null);
        }

        if (motivo == Merma.MotivoOtro && string.IsNullOrWhiteSpace(observacion))
        {
            return (false, "Indique en la observación el motivo de la baja.", null);
        }

        try
        {
            var producto = _productoRepository.ObtenerPorId(idProducto);
            if (producto is null)
            {
                return (false, "El producto seleccionado no existe.", null);
            }

            var valCantidad = Validaciones.ValidarCantidad(cantidad, producto.Unidad);
            if (!valCantidad.EsValido)
            {
                return (false, valCantidad.Mensaje, null);
            }

            var valExistencia = Validaciones.ValidarExistenciaSuficiente(producto.Existencia, cantidad);
            if (!valExistencia.EsValido)
            {
                return (false, $"No se pueden dar de baja {Cantidades.Formatear(cantidad, producto.Unidad)}: " +
                               $"solo hay {Cantidades.Formatear(producto.Existencia, producto.Unidad)} en existencia.", null);
            }

            var merma = new Merma
            {
                FechaHora      = DateTime.Now,
                IdProducto     = producto.IdProducto,
                NombreProducto = producto.Nombre,
                Cantidad       = cantidad,
                Motivo         = motivo,
                CostoUnitario  = producto.PrecioCompra,
                Observacion    = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim(),
                IdUsuario      = Sesion.IdUsuario
            };

            merma.IdMerma = _mermaRepository.Registrar(merma);
            return (true, $"Baja registrada: {Cantidades.Formatear(cantidad, producto.Unidad)} × {producto.Nombre} ({motivo}). Inventario actualizado.", merma);
        }
        catch (ReglaNegocioException ex)
        {
            // Existencia insuficiente detectada dentro de la transacción.
            return (false, $"{ex.Message} La baja no se registró.", null);
        }
        catch (Exception ex) when (ErroresBaseDatos.EsReglaDeIntegridad(ex))
        {
            return (false, $"{ErroresBaseDatos.MensajeAmigable(ex)} La baja no se registró.", null);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Registro de merma (producto #{idProducto}, cantidad {cantidad})", ex);
            return (false, $"No se pudo registrar la baja. No se guardó ningún cambio.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}", null);
        }
    }

    public List<Merma> Consultar(DateTime? desde = null, DateTime? hasta = null)
    {
        return _mermaRepository.ObtenerPorRango(desde, hasta);
    }
}
