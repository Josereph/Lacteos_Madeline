using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Reglas de negocio del módulo de productos: registro, modificación,
/// búsqueda y cambio de estado, con las validaciones exigidas por la
/// documentación (nombre obligatorio, precios y existencias no negativas).
/// </summary>
public class ProductoService
{
    private readonly ProductoRepository _productoRepository = new();

    public (bool Exitoso, string Mensaje, int IdProducto) Registrar(Producto producto)
    {
        var validacion = Validar(producto);
        if (!validacion.EsValido)
        {
            return (false, validacion.Mensaje, 0);
        }

        var id = _productoRepository.Registrar(producto);
        return (true, "Producto registrado correctamente.", id);
    }

    public (bool Exitoso, string Mensaje) Modificar(Producto producto)
    {
        var validacion = Validar(producto);
        if (!validacion.EsValido)
        {
            return (false, validacion.Mensaje);
        }

        _productoRepository.Modificar(producto);
        return (true, "Producto modificado correctamente.");
    }

    public void CambiarEstado(int idProducto, bool estado)
    {
        _productoRepository.CambiarEstado(idProducto, estado);
    }

    public List<Producto> Buscar(string filtro = "", bool soloActivos = false)
    {
        return _productoRepository.Buscar(filtro, soloActivos);
    }

    public Producto? ObtenerPorId(int idProducto)
    {
        return _productoRepository.ObtenerPorId(idProducto);
    }

    private static (bool EsValido, string Mensaje) Validar(Producto producto)
    {
        var validacionNombre = Validaciones.ValidarTexto(producto.Nombre, "nombre del producto");
        if (!validacionNombre.EsValido)
        {
            return validacionNombre;
        }

        if (producto.IdCategoria <= 0)
        {
            return (false, "Debe seleccionar una categoría.");
        }

        var validacionPrecio = Validaciones.ValidarPrecio(producto.PrecioVenta, "precio de venta");
        if (!validacionPrecio.EsValido)
        {
            return validacionPrecio;
        }

        if (producto.PrecioCompra is < 0)
        {
            return (false, "El precio de compra no puede ser negativo.");
        }

        if (producto.Existencia < 0)
        {
            return (false, "La existencia no puede ser negativa.");
        }

        if (producto.StockMinimo < 0)
        {
            return (false, "El stock mínimo no puede ser negativo.");
        }

        return (true, string.Empty);
    }
}
