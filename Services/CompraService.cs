using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Orquesta el flujo de una compra: arma el detalle en memoria,
/// valida cada línea y al confirmar registra la compra completa
/// aumentando el inventario de forma atómica (sección 5 lineamientos).
/// </summary>
public class CompraService
{
    private readonly ProductoRepository _productoRepository = new();
    private readonly ProveedorRepository _proveedorRepository = new();
    private readonly CompraRepository _compraRepository = new();

    private readonly List<DetalleCompra> _detalle = new();

    public IReadOnlyList<DetalleCompra> Detalle => _detalle;

    public decimal Total => _detalle.Sum(d => d.Subtotal);

    public void IniciarCompra()
    {
        _detalle.Clear();
    }

    public (bool Exitoso, string Mensaje) AgregarProducto(int idProducto, int cantidad, decimal costoUnitario)
    {
        var valCantidad = Validaciones.ValidarCantidad(cantidad);
        if (!valCantidad.EsValido)
        {
            return (false, valCantidad.Mensaje);
        }

        var valCosto = Validaciones.ValidarCosto(costoUnitario);
        if (!valCosto.EsValido)
        {
            return (false, valCosto.Mensaje);
        }

        var producto = _productoRepository.ObtenerPorId(idProducto);
        if (producto is null)
        {
            return (false, "El producto seleccionado no existe.");
        }

        // Si el producto ya está en el detalle, se acumula la cantidad
        var lineaExistente = _detalle.FirstOrDefault(d => d.IdProducto == idProducto);
        if (lineaExistente is not null)
        {
            lineaExistente.Cantidad += cantidad;
        }
        else
        {
            _detalle.Add(new DetalleCompra
            {
                IdProducto     = producto.IdProducto,
                NombreProducto = producto.Nombre,
                Cantidad       = cantidad,
                CostoUnitario  = costoUnitario
            });
        }

        return (true, string.Empty);
    }

    public void EliminarProducto(int idProducto)
    {
        _detalle.RemoveAll(d => d.IdProducto == idProducto);
    }

    public (bool Exitoso, string Mensaje, Compra? CompraRegistrada) ConfirmarCompra(int idProveedor)
    {
        if (_detalle.Count == 0)
        {
            return (false, "Debe agregar al menos un producto a la compra.", null);
        }

        var proveedor = _proveedorRepository.ObtenerPorId(idProveedor);
        if (proveedor is null || !proveedor.Estado)
        {
            return (false, "El proveedor seleccionado no es válido o está inactivo.", null);
        }

        var compra = new Compra
        {
            FechaHora       = DateTime.Now,
            IdProveedor     = idProveedor,
            NombreProveedor = proveedor.Nombre,
            Total           = Total,
            Detalles        = new List<DetalleCompra>(_detalle)
        };

        try
        {
            var idCompra = _compraRepository.RegistrarCompraCompleta(compra);
            compra.IdCompra = idCompra;
            _detalle.Clear();
            return (true, "Compra registrada correctamente. Inventario actualizado.", compra);
        }
        catch (Exception ex)
        {
            return (false, $"Error al registrar la compra: {ex.Message}", null);
        }
    }
}
