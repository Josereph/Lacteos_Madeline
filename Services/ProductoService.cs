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

        if (producto.Existencia > 0)
        {
            var validacionExistencia = Validaciones.ValidarCantidad(producto.Existencia, producto.Unidad);
            if (!validacionExistencia.EsValido)
            {
                return (false, $"Existencia inicial: {validacionExistencia.Mensaje}", 0);
            }
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
            var actual = _productoRepository.ObtenerPorId(producto.IdProducto);
            if (actual is null)
            {
                return (false, "El producto ya no existe.");
            }

            if (!UnidadMedida.PermiteDecimales(producto.Unidad) && actual.Existencia != decimal.Truncate(actual.Existencia))
            {
                return (false, $"No se puede cambiar a \"{producto.Unidad}\": la existencia actual ({Cantidades.ParaEditar(actual.Existencia)}) tiene decimales. Ajústela primero.");
            }

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

    /// <summary>Producto con ese código exacto, o null.</summary>
    public Producto? ObtenerPorCodigo(string codigo)
    {
        return string.IsNullOrWhiteSpace(codigo) ? null : _productoRepository.ObtenerPorCodigo(codigo);
    }

    /// <summary>Categorías activas, para asignarlas a un producto.</summary>
    public List<Categoria> CategoriasActivas()
    {
        return _categoriaRepository.ObtenerTodas(incluirInactivas: false);
    }

    private (bool EsValido, string Mensaje) Validar(Producto producto)
    {
        var validacionNombre = Validaciones.ValidarTexto(producto.Nombre, "nombre del producto");
        if (!validacionNombre.EsValido)
        {
            return validacionNombre;
        }

        if (!UnidadMedida.EsValida(producto.Unidad))
        {
            return (false, "Seleccione la unidad de venta del producto.");
        }

        producto.Codigo = string.IsNullOrWhiteSpace(producto.Codigo) ? null : producto.Codigo.Trim();
        if (producto.Codigo is not null && _productoRepository.ExisteCodigo(producto.Codigo, producto.IdProducto))
        {
            return (false, $"El código \"{producto.Codigo}\" ya pertenece a otro producto.");
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

        if (producto.StockMinimo > 0)
        {
            var validacionMinimo = Validaciones.ValidarCantidad(producto.StockMinimo, producto.Unidad);
            if (!validacionMinimo.EsValido)
            {
                return (false, $"Stock mínimo: {validacionMinimo.Mensaje}");
            }
        }

        return (true, string.Empty);
    }
}
