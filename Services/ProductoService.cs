using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Reglas de negocio del módulo de productos: registro, modificación,
/// búsqueda y cambio de estado, con las validaciones exigidas por la
/// documentación (nombre obligatorio, precios y existencias no negativas).
/// La existencia de un producto ya registrado se corrige con
/// <see cref="InventarioService.AjustarExistencia"/>, nunca al modificarlo.
/// </summary>
public class ProductoService
{
    private readonly ProductoRepository _productoRepository = new();
    private readonly CategoriaRepository _categoriaRepository = new();

    public (bool Exitoso, string Mensaje, int IdProducto) Registrar(Producto producto)
    {
        var validacion = Validar(producto);
        if (!validacion.EsValido)
        {
            return (false, validacion.Mensaje, 0);
        }

        if (producto.Existencia < 0)
        {
            return (false, "La existencia inicial no puede ser negativa.", 0);
        }

        try
        {
            var id = _productoRepository.Registrar(producto);
            return (true, "Producto registrado correctamente.", id);
        }
        catch (Exception ex) when (ErroresBaseDatos.EsReglaDeIntegridad(ex))
        {
            return (false, ErroresBaseDatos.MensajeAmigable(ex), 0);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Registrar producto \"{producto.Nombre}\"", ex);
            return (false, $"No se pudo registrar el producto.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}", 0);
        }
    }

    /// <summary>
    /// Modifica los datos del producto. <see cref="Producto.Existencia"/>
    /// se ignora: la existencia solo cambia con un ajuste registrado.
    /// </summary>
    public (bool Exitoso, string Mensaje) Modificar(Producto producto)
    {
        var validacion = Validar(producto);
        if (!validacion.EsValido)
        {
            return (false, validacion.Mensaje);
        }

        try
        {
            _productoRepository.Modificar(producto);
            return (true, "Producto modificado correctamente.");
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
            LogHelper.Error($"Modificar producto #{producto.IdProducto}", ex);
            return (false, $"No se pudo modificar el producto.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
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

    /// <summary>Categorías activas, para asignarlas a un producto.</summary>
    public List<Categoria> CategoriasActivas()
    {
        return _categoriaRepository.ObtenerTodas(incluirInactivas: false);
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

        if (Dinero.TieneMasDeDosDecimales(producto.PrecioVenta) ||
            (producto.PrecioCompra is decimal compra && Dinero.TieneMasDeDosDecimales(compra)))
        {
            return (false, "Los precios no pueden tener más de dos decimales.");
        }

        if (producto.StockMinimo < 0)
        {
            return (false, "El stock mínimo no puede ser negativo.");
        }

        return (true, string.Empty);
    }
}
