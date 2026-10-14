using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Movimientos de inventario que no son ventas ni compras: mermas y
/// ajustes por conteo físico.
/// </summary>
public class InventarioTests : PruebaConBaseDatos
{
    [Fact]
    public void Merma_DescuentaExistenciaYConservaElCosto()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10, precioCompra: 2.25m);

        var resultado = new MermaService().Registrar(idProducto, 3, Merma.MotivoCaducidad, null);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(7, Existencia(idProducto));
        var merma = Assert.Single(new MermaService().Consultar());
        Assert.Equal(2.25m, merma.CostoUnitario);
        Assert.Equal(6.75m, merma.CostoTotal);
    }

    [Fact]
    public void Merma_NoAfectaVentasNiCaja()
    {
        var caja = AbrirCaja(10m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);

        new MermaService().Registrar(idProducto, 2, Merma.MotivoDanio, null);

        Assert.Equal(0, Contar("Ventas"));
        Assert.Equal(10m, new CajaService().ObtenerResumen(caja.IdCaja).SaldoEsperado);
    }

    [Fact]
    public void MermaMayorQueLaExistencia_SeRechazaSinGuardarNada()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 2);

        var resultado = new MermaService().Registrar(idProducto, 3, Merma.MotivoCaducidad, null);

        Assert.False(resultado.Exitoso);
        Assert.Equal(2, Existencia(idProducto));
        Assert.Equal(0, Contar("Mermas"));
    }

    [Fact]
    public void MermaPorOtroMotivo_ExigeObservacion()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 5);

        Assert.False(new MermaService().Registrar(idProducto, 1, Merma.MotivoOtro, "  ").Exitoso);
        Assert.True(new MermaService().Registrar(idProducto, 1, Merma.MotivoOtro, "Se cayó al piso").Exitoso);
    }

    [Fact]
    public void AjustePorConteoFisico_FijaLaExistencia()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);

        Assert.True(new InventarioService().AjustarExistencia(idProducto, 8).Exitoso);
        Assert.Equal(8, Existencia(idProducto));
    }

    [Fact]
    public void AjusteNegativo_SeRechaza()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);

        Assert.False(new InventarioService().AjustarExistencia(idProducto, -1).Exitoso);
        Assert.Equal(10, Existencia(idProducto));
    }

    [Fact]
    public void BajoStock_IncluyeAgotadosPrimero()
    {
        var idAgotado = CrearProducto("Agotado", 1m, 0);
        var idNormal = CrearProducto("Con existencia", 1m, 50);
        Ejecutar($"UPDATE Productos SET StockMinimo = 5 WHERE IdProducto IN ({idAgotado}, {idNormal});");
        var idBajo = CrearProducto("Bajo", 1m, 3);
        Ejecutar($"UPDATE Productos SET StockMinimo = 5 WHERE IdProducto = {idBajo};");

        var bajos = new InventarioService().ConsultarBajoStock();

        Assert.Equal([idAgotado, idBajo], bajos.Select(p => p.IdProducto));
    }
}
