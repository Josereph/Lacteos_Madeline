using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Consultas relacionadas con el inventario. El descuento de existencias
/// por venta ocurre dentro de VentaRepository, como parte de la misma
/// transacción que registra la venta, para que el inventario nunca quede
/// en un estado inconsistente.
/// </summary>
public class InventarioService
{
    public const string MotivoConteoFisico = "Conteo físico";
    public const string MotivoCorreccionProducto = "Corrección desde Productos";

    private readonly ProductoRepository _productoRepository = new();

    public List<Producto> ConsultarDisponibles()
    {
        return _productoRepository.Buscar(soloActivos: true);
    }

    /// <summary>
    /// Existencias críticas, filtradas y ordenadas por urgencia en SQL.
    /// </summary>
    public List<Producto> ConsultarBajoStock()
    {
        return _productoRepository.ObtenerBajoStock();
    }

    public decimal ConsultarExistencia(int idProducto)
    {
        var producto = _productoRepository.ObtenerPorId(idProducto);
        return producto?.Existencia ?? 0;
    }

    public bool HayExistenciaSuficiente(int idProducto, decimal cantidad)
    {
        return ConsultarExistencia(idProducto) >= cantidad;
    }

    /// <summary>
    /// Ajuste manual de existencia. Queda registrado en AjustesInventario
    /// (valor anterior, nuevo y motivo) en la misma transacción que el cambio.
    /// Con <paramref name="existenciaEsperada"/> (la que el usuario veía en
    /// pantalla) el ajuste se rechaza si entretanto hubo una venta o compra.
    /// Para pérdidas por caducidad o daño use MermaService, que además
    /// valora la pérdida.
    /// </summary>
    public (bool Exitoso, string Mensaje) AjustarExistencia(
        int idProducto, decimal nuevaExistencia, string motivo = MotivoConteoFisico, decimal? existenciaEsperada = null)
    {
        if (nuevaExistencia < 0)
        {
            return (false, "La existencia no puede ser negativa.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return (false, "Indique el motivo del ajuste.");
        }

        try
        {
            var producto = _productoRepository.ObtenerPorId(idProducto);
            if (producto is null)
            {
                return (false, "El producto no existe.");
            }

            if (nuevaExistencia > 0)
            {
                var valCantidad = Validaciones.ValidarCantidad(nuevaExistencia, producto.Unidad);
                if (!valCantidad.EsValido)
                {
                    return (false, valCantidad.Mensaje);
                }
            }

            var anterior = _productoRepository.AjustarExistencia(
                idProducto, nuevaExistencia, motivo.Trim(), existenciaEsperada, Sesion.IdUsuario);
            return (true, anterior == Cantidades.Redondear(nuevaExistencia)
                ? "La existencia no cambió."
                : $"Existencia ajustada de {Cantidades.Formatear(anterior, producto.Unidad)} a {Cantidades.Formatear(nuevaExistencia, producto.Unidad)}.");
        }
        catch (ReglaNegocioException ex)
        {
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Ajuste de existencia producto #{idProducto}", ex);
            return (false, ErroresBaseDatos.MensajeAmigable(ex));
        }
    }
}
