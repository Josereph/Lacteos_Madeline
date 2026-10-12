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

    public int ConsultarExistencia(int idProducto)
    {
        var producto = _productoRepository.ObtenerPorId(idProducto);
        return producto?.Existencia ?? 0;
    }

    public bool HayExistenciaSuficiente(int idProducto, int cantidad)
    {
        return ConsultarExistencia(idProducto) >= cantidad;
    }

    /// <summary>
    /// Ajuste por conteo físico. Para pérdidas por caducidad o daño use
    /// MermaService, que deja registro del motivo y del costo.
    /// </summary>
    public (bool Exitoso, string Mensaje) AjustarExistencia(int idProducto, int nuevaExistencia)
    {
        if (nuevaExistencia < 0)
        {
            return (false, "La existencia no puede ser negativa.");
        }

        try
        {
            var anterior = ConsultarExistencia(idProducto);
            _productoRepository.AjustarExistencia(idProducto, nuevaExistencia);
            LogHelper.Info("Ajuste de inventario", $"Producto #{idProducto}: {anterior} → {nuevaExistencia}");
            return (true, "Existencia ajustada correctamente.");
        }
        catch (Exception ex)
        {
            if (ex is not InvalidOperationException) LogHelper.Error($"Ajuste de existencia producto #{idProducto}", ex);
            return (false, ErroresBaseDatos.MensajeAmigable(ex));
        }
    }
}
