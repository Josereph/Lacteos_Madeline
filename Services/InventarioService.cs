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

    public List<Producto> ConsultarBajoStock()
    {
        return _productoRepository.Buscar(soloActivos: true)
            .Where(p => p.Existencia <= p.StockMinimo)
            .ToList();
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

    public void AjustarExistencia(int idProducto, int nuevaExistencia)
    {
        _productoRepository.AjustarExistencia(idProducto, nuevaExistencia);
    }
}
