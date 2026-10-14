using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Reportes: filtros de fecha, acumulados y productos más vendidos.
/// </summary>
public class ReportesTests : PruebaConBaseDatos
{
    [Fact]
    public void ReporteDeVentas_EstaEnOrdenCronologicoConAcumulado()
    {
        AbrirCaja();
        var idQueso = CrearProducto("Queso fresco", 3.50m, 10);
        var idCrema = CrearProducto("Crema", 2.25m, 10);
        Assert.True(Vender(idQueso, 2).Exitoso); // 7.00
        Assert.True(Vender(idCrema, 1).Exitoso); // 2.25

        var filas = new ReporteService().ReporteVentas(DateTime.Today, DateTime.Today);

        Assert.Equal(2, filas.Count);
        Assert.True(filas[0].IdVenta < filas[1].IdVenta);
        Assert.Equal(7.00m, filas[0].Acumulado);
        Assert.Equal(9.25m, filas[1].Acumulado);
    }

    [Fact]
    public void ReporteDeVentas_ExcluyeDiasFueraDelRango()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        Assert.True(Vender(idProducto, 1).Exitoso);

        Assert.Empty(new ReporteService().ReporteVentas(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1)));
        Assert.Single(new ReporteService().ReporteVentas(DateTime.Today, null));
    }

    [Fact]
    public void ProductosMasVendidos_OrdenaPorUnidadesConImporteExacto()
    {
        AbrirCaja();
        var idQueso = CrearProducto("Queso fresco", 3.50m, 20);
        var idDulce = CrearProducto("Dulce de leche", 0.10m, 20);
        Assert.True(Vender(idQueso, 2).Exitoso);
        for (var i = 0; i < 3; i++)
        {
            Assert.True(Vender(idDulce, 1).Exitoso);
        }

        var filas = new ReporteService().ProductosMasVendidos();

        Assert.Equal(["Dulce de leche", "Queso fresco"], filas.Select(f => f.Producto));
        Assert.Equal(3, filas[0].UnidadesVendidas);
        Assert.Equal(0.30m, filas[0].TotalVendido);
        Assert.Equal(3, filas[0].NumeroVentas);
        Assert.Equal(17, filas[0].ExistenciaActual);
        Assert.Equal(7.00m, filas[1].TotalVendido);
    }

    [Fact]
    public void ReporteDeMermas_ValoraLaPerdida()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10, precioCompra: 2.10m);
        Assert.True(new MermaService().Registrar(idProducto, 3, Merma.MotivoDanio, null).Exitoso);

        var merma = Assert.Single(new ReporteService().ReporteMermas(DateTime.Today, DateTime.Today));

        Assert.Equal(6.30m, merma.CostoTotal);
    }
}
