using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Tests;

/// <summary>
/// Apertura, movimientos, arqueo y cierre de caja.
/// </summary>
public class CajaTests : PruebaConBaseDatos
{
    [Fact]
    public void NoSePuedenTenerDosCajasAbiertas()
    {
        AbrirCaja(10m);

        var resultado = new CajaService().AbrirCaja(5m);

        Assert.False(resultado.Exitoso);
        Assert.Equal(1, Contar("Cajas"));
    }

    [Fact]
    public void FondoInicialNegativo_SeRechaza()
    {
        Assert.False(new CajaService().AbrirCaja(-1m).Exitoso);
        Assert.Equal(0, Contar("Cajas"));
    }

    /// <summary>
    /// Esperado = fondo + ventas en efectivo + entradas − salidas.
    /// Las ventas con tarjeta no forman parte del efectivo.
    /// </summary>
    [Fact]
    public void Cierre_CalculaYGuardaElArqueo()
    {
        var caja = AbrirCaja(50m);
        var servicio = new CajaService();
        var idQueso = CrearProducto("Queso fresco", 5.00m, 20);
        var idCrema = CrearProducto("Crema", 7.00m, 20);

        Assert.True(Vender(idQueso, 2).Exitoso);                     // +10 efectivo
        var tarjeta = new VentaService();
        tarjeta.AgregarProducto(idCrema, 1);
        Assert.True(tarjeta.ConfirmarVenta(Venta.PagoTarjeta, null).Exitoso); // 7 tarjeta
        Assert.True(servicio.RegistrarMovimiento(caja.IdCaja, "Entrada", 20m, "Cambio").Exitoso);
        Assert.True(servicio.RegistrarMovimiento(caja.IdCaja, "Salida", 15m, "Pago de hielo").Exitoso);

        var resultado = servicio.CerrarCaja(caja.IdCaja, 60m);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var resumen = resultado.Resumen!;
        Assert.Equal(10m, resumen.VentasEfectivo);
        Assert.Equal(7m, resumen.VentasOtrosMetodos);
        Assert.Equal(65m, resumen.SaldoEsperado);
        Assert.Equal("Cerrada", resumen.Caja.Estado);
        Assert.Equal(60m, resumen.Caja.MontoFinal);
        Assert.Equal(65m, resumen.Caja.MontoEsperado);
        Assert.Equal(-5m, resumen.Caja.Diferencia);
    }

    [Fact]
    public void Cierre_GeneraUnRespaldoAutomaticoVerificado()
    {
        var caja = AbrirCaja(10m);

        var resultado = new CajaService().CerrarCaja(caja.IdCaja, 10m);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Contains("Respaldo automático generado", resultado.Mensaje);
        Assert.Single(Directory.GetFiles(BackupHelper.CarpetaRespaldosAutomaticos, $"*cierre-caja-{caja.IdCaja}.db"));
    }

    [Fact]
    public void SalidaMayorAlEfectivoDisponible_SeRechaza()
    {
        var caja = AbrirCaja(20m);

        var resultado = new CajaService().RegistrarMovimiento(caja.IdCaja, "Salida", 20.01m, null);

        Assert.False(resultado.Exitoso);
        Assert.Equal(0, Contar("MovimientosCaja"));
    }

    [Fact]
    public void MontoConMasDeDosDecimales_SeRechaza()
    {
        var caja = AbrirCaja(20m);

        Assert.False(new CajaService().RegistrarMovimiento(caja.IdCaja, "Entrada", 1.005m, null).Exitoso);
    }

    [Fact]
    public void CajaCerrada_NoAdmiteMovimientosNiVentasNiSegundoCierre()
    {
        var caja = AbrirCaja(10m);
        var servicio = new CajaService();
        Assert.True(servicio.CerrarCaja(caja.IdCaja, 10m).Exitoso);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 5);

        Assert.False(servicio.RegistrarMovimiento(caja.IdCaja, "Entrada", 5m, null).Exitoso);
        Assert.False(Vender(idProducto, 1).Exitoso);
        Assert.False(servicio.CerrarCaja(caja.IdCaja, 99m).Exitoso);
        Assert.Equal(10m, servicio.ObtenerResumen(caja.IdCaja).Caja.MontoFinal);
    }

    [Fact]
    public void CajaCerrada_EsInmutableAunqueSeIntenteModificarDirectamente()
    {
        var caja = AbrirCaja(10m);
        new CajaService().CerrarCaja(caja.IdCaja, 10m);

        var error = Assert.Throws<SqliteException>(() =>
            Ejecutar($"UPDATE Cajas SET MontoFinal = 1000 WHERE IdCaja = {caja.IdCaja};"));

        Assert.Equal(19, error.SqliteErrorCode);
    }

    /// <summary>
    /// 0.10 + 0.10 + 0.10 no es exacto en punto flotante; como los montos
    /// se guardan en centavos enteros, el arqueo cuadra al centavo.
    /// </summary>
    [Fact]
    public void SumasDeCentavos_CuadranExactamente()
    {
        var caja = AbrirCaja(0m);
        var idProducto = CrearProducto("Dulce de leche", 0.10m, 10);
        for (var i = 0; i < 3; i++)
        {
            Assert.True(Vender(idProducto, 1).Exitoso);
        }

        var resultado = new CajaService().CerrarCaja(caja.IdCaja, 0.30m);

        Assert.Equal(0.30m, resultado.Resumen!.SaldoEsperado);
        Assert.Equal(0m, resultado.Resumen.Caja.Diferencia);
    }
}
