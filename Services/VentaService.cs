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

    public (bool Exitoso, string Mensaje, Venta? VentaRegistrada) ConfirmarVenta()
    {
        if (_carrito.Count == 0)
        {
            return (false, "Debe agregar al menos un producto a la venta.", null);
        }

        var venta = new Venta
        {
            FechaHora = DateTime.Now,
            Total = Total,
            Detalles = new List<DetalleVenta>(_carrito)
        };

        try
        {
            var idVenta = _ventaRepository.RegistrarVentaCompleta(venta);
            venta.IdVenta = idVenta;
            _carrito.Clear();
            return (true, "Venta registrada correctamente.", venta);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message, null);
        }
    }
}
