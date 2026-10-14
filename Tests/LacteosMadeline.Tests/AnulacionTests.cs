using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Tests;

/// <summary>
/// Anulación de ventas: devuelve existencias, cuadra la caja (en la misma
/// jornada o con una salida en la jornada siguiente), queda registrada y
/// solo un administrador (o un cajero autorizado) puede hacerla.
/// </summary>
public class AnulacionTests : PruebaConBaseDatos
{
    [Fact]
    public void AnularEnLaMismaJornada_DevuelveExistenciaYSacaLaVentaDelArqueo()
    {
        var caja = AbrirCaja(10m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var venta = Vender(idProducto, 2).VentaRegistrada!;
        Assert.Equal(17.00m, new CajaService().ObtenerResumen(caja.IdCaja).SaldoEsperado);

        var resultado = new VentaService().Anular(venta.IdVenta, "Error del cajero");

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(10, Existencia(idProducto));
        var resumen = new CajaService().ObtenerResumen(caja.IdCaja);
        Assert.Equal(10.00m, resumen.SaldoEsperado);
        Assert.Equal(0, resumen.NumeroVentas);
        Assert.Equal(1, resumen.VentasAnuladas);
        Assert.Equal(0, Contar("MovimientosCaja"));

        var anulada = new VentaRepository().ObtenerPorId(venta.IdVenta)!;
        Assert.True(anulada.EsAnulada);
        Assert.Equal("Error del cajero", anulada.MotivoAnulacion);
        Assert.Equal(caja.IdCaja, anulada.IdCajaAnulacion);
        Assert.Contains("ANULADA", TicketHelper.GenerarTexto(anulada));
    }

    [Fact]
    public void AnularVentaDeUnaCajaCerrada_RegistraLaDevolucionComoSalidaYNoAlteraElCierre()
    {
        var cajaLunes = AbrirCaja(0m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var venta = Vender(idProducto, 2).VentaRegistrada!; // 7.00 en efectivo
        new CajaService().CerrarCaja(cajaLunes.IdCaja, 7.00m);
        var cajaMartes = AbrirCaja(20m);

        var resultado = new VentaService().Anular(venta.IdVenta, "Cliente devolvió el producto");

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(10, Existencia(idProducto));

        var martes = new CajaService().ObtenerResumen(cajaMartes.IdCaja);
        Assert.Equal(7.00m, martes.TotalSalidas);
        Assert.Equal(13.00m, martes.SaldoEsperado);
        Assert.Contains("#" + venta.IdVenta, Assert.Single(martes.Movimientos).Descripcion);

        // El cierre del lunes ya contado no cambia.
        var lunes = new CajaService().ObtenerResumen(cajaLunes.IdCaja);
        Assert.Equal(7.00m, lunes.VentasEfectivo);
        Assert.Equal(0m, lunes.Caja.Diferencia);
    }

    [Fact]
    public void AnularVentaAnteriorSinEfectivoSuficiente_SeRechazaSinTocarNada()
    {
        var cajaLunes = AbrirCaja(0m);
        var idProducto = CrearProducto("Queso fresco", 10m, 10);
        var venta = Vender(idProducto, 2).VentaRegistrada!; // 20.00
        new CajaService().CerrarCaja(cajaLunes.IdCaja, 20m);
        AbrirCaja(5m);

        var resultado = new VentaService().Anular(venta.IdVenta, "Cliente devolvió el producto");

        Assert.False(resultado.Exitoso);
        Assert.Contains("efectivo suficiente", resultado.Mensaje);
        Assert.Equal(8, Existencia(idProducto));
        Assert.False(new VentaRepository().ObtenerPorId(venta.IdVenta)!.EsAnulada);
    }

    [Fact]
    public void AnularVentaAnteriorConTarjeta_NoMueveEfectivo()
    {
        var cajaLunes = AbrirCaja(0m);
        var idProducto = CrearProducto("Queso fresco", 10m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 1);
        var venta = servicio.ConfirmarVenta(Venta.PagoTarjeta, null).VentaRegistrada!;
        new CajaService().CerrarCaja(cajaLunes.IdCaja, 0m);
        var cajaMartes = AbrirCaja(0m);

        Assert.True(new VentaService().Anular(venta.IdVenta, "Cargo duplicado en la tarjeta").Exitoso);

        Assert.Equal(0m, new CajaService().ObtenerResumen(cajaMartes.IdCaja).TotalSalidas);
    }

    [Fact]
    public void AnularDosVeces_OSinCajaAbierta_OSinMotivo_SeRechaza()
    {
        var caja = AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3m, 10);
        var venta = Vender(idProducto, 1).VentaRegistrada!;
        var servicio = new VentaService();

        Assert.False(servicio.Anular(venta.IdVenta, "  ").Exitoso);
        Assert.True(servicio.Anular(venta.IdVenta, "Error al cobrar").Exitoso);
        Assert.False(servicio.Anular(venta.IdVenta, "Error al cobrar").Exitoso);
        Assert.Equal(10, Existencia(idProducto)); // no devolvió dos veces

        var otra = Vender(idProducto, 1).VentaRegistrada!;
        new CajaService().CerrarCaja(caja.IdCaja, 3m);
        var sinCaja = servicio.Anular(otra.IdVenta, "Error al cobrar");
        Assert.False(sinCaja.Exitoso);
        Assert.Contains("Abra la caja", sinCaja.Mensaje);
    }

    [Fact]
    public void CajeroSinAutorizacion_NoPuedeAnular()
    {
        var cajero = CrearUsuario("Cajero", Usuario.RolCajero);
        var admin = CrearUsuario("Admin", Usuario.RolAdministrador);
        Sesion.Iniciar(cajero);
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3m, 10);
        var venta = Vender(idProducto, 1).VentaRegistrada!;

        Assert.False(new VentaService().Anular(venta.IdVenta, "Error al cobrar").Exitoso);

        Assert.True(new VentaService().Anular(venta.IdVenta, "Error al cobrar", idAutoriza: admin.IdUsuario).Exitoso);
        Assert.Equal("Admin", new VentaRepository().ObtenerPorId(venta.IdVenta)!.NombreUsuarioAnulacion);
    }

    [Fact]
    public void VentasAnuladas_NoCuentanEnReportes()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3m, 10, precioCompra: 1m);
        Vender(idProducto, 1);
        var anulada = Vender(idProducto, 4).VentaRegistrada!;
        new VentaService().Anular(anulada.IdVenta, "Error al cobrar");

        var reporte = new ReporteService();
        Assert.Equal(1, Assert.Single(reporte.ProductosMasVendidos()).UnidadesVendidas);
        Assert.Equal(3m, Assert.Single(reporte.ReporteUtilidad()).Ventas);
        var filas = reporte.ReporteVentas();
        Assert.Equal(2, filas.Count);
        Assert.Equal(3m, filas[^1].Acumulado);
    }

    [Fact]
    public void LaBaseDeDatosImpideEditarOBorrarUnaVenta()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3m, 10);
        var venta = Vender(idProducto, 1).VentaRegistrada!;

        var editar = Assert.Throws<SqliteException>(() => Ejecutar($"UPDATE Ventas SET Total = 1 WHERE IdVenta = {venta.IdVenta};"));
        var borrar = Assert.Throws<SqliteException>(() => Ejecutar($"DELETE FROM DetalleVentas WHERE IdVenta = {venta.IdVenta};"));

        Assert.Equal(19, editar.SqliteErrorCode);
        Assert.Equal(19, borrar.SqliteErrorCode);
    }
}
