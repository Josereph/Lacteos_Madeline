using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Flujo de compra: aumenta existencias de forma atómica y conserva el
/// costo histórico de cada línea.
/// </summary>
public class ComprasTests : PruebaConBaseDatos
{
    [Fact]
    public void CompraConfirmada_AumentaExistenciasYGuardaElDetalle()
    {
        var idProveedor = CrearProveedor();
        var idA = CrearProducto("Queso fresco", 3.50m, 2);
        var idB = CrearProducto("Crema", 2.00m, 0);
        var servicio = new CompraService();
        servicio.AgregarProducto(idA, 10, 2.10m);
        servicio.AgregarProducto(idB, 6, 1.25m);

        var resultado = servicio.ConfirmarCompra(idProveedor);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(12, Existencia(idA));
        Assert.Equal(6, Existencia(idB));

        var guardada = new CompraRepository().ObtenerPorId(resultado.CompraRegistrada!.IdCompra)!;
        Assert.Equal(28.50m, guardada.Total);
        Assert.Equal(2, guardada.Detalles.Count);
    }

    [Fact]
    public void MismoProductoDosVeces_AcumulaLaCantidadEnUnaLinea()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 0);
        var servicio = new CompraService();

        servicio.AgregarProducto(idProducto, 4, 2m);
        servicio.AgregarProducto(idProducto, 3, 2m);

        Assert.Equal(7, Assert.Single(servicio.Detalle).Cantidad);
    }

    [Fact]
    public void ProveedorInactivo_SeRechaza()
    {
        var idProveedor = CrearProveedor(activo: false);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 0);
        var servicio = new CompraService();
        servicio.AgregarProducto(idProducto, 5, 2m);

        var resultado = servicio.ConfirmarCompra(idProveedor);

        Assert.False(resultado.Exitoso);
        Assert.Equal(0, Existencia(idProducto));
        Assert.Equal(0, Contar("Compras"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, -1)]
    public void CantidadOCostoInvalido_SeRechaza(int cantidad, decimal costo)
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 0);

        Assert.False(new CompraService().AgregarProducto(idProducto, cantidad, costo).Exitoso);
    }

    /// <summary>
    /// Si un producto desaparece antes de confirmar, la compra completa se
    /// revierte: la otra línea tampoco aumenta su existencia.
    /// </summary>
    [Fact]
    public void FalloEnUnaLinea_RevierteLaCompraCompleta()
    {
        var idProveedor = CrearProveedor();
        var idA = CrearProducto("Queso fresco", 3.50m, 2);
        var idB = CrearProducto("Crema", 2.00m, 0);
        var servicio = new CompraService();
        servicio.AgregarProducto(idA, 10, 2m);
        servicio.AgregarProducto(idB, 5, 1m);

        Ejecutar($"DELETE FROM Productos WHERE IdProducto = {idB};");

        var resultado = servicio.ConfirmarCompra(idProveedor);

        Assert.False(resultado.Exitoso);
        Assert.Equal(2, Existencia(idA));
        Assert.Equal(0, Contar("Compras"));
        Assert.Equal(0, Contar("DetalleCompras"));
    }
}
