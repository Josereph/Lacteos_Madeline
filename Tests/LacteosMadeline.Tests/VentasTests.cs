using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Flujo de venta: validaciones, descuento de inventario, efectivo y
/// atomicidad (una venta fallida no deja rastros ni descuenta nada).
/// </summary>
public class VentasTests : PruebaConBaseDatos
{
    [Fact]
    public void SinCajaAbierta_NoSeRegistraLaVentaNiSeDescuentaInventario()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);

        var resultado = Vender(idProducto, 2);

        Assert.False(resultado.Exitoso);
        Assert.Contains("caja abierta", resultado.Mensaje);
        Assert.Equal(10, Existencia(idProducto));
        Assert.Equal(0, Contar("Ventas"));
    }

    [Fact]
    public void VentaEnEfectivo_DescuentaInventarioYCalculaElCambio()
    {
        var caja = AbrirCaja(20m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 3);

        var resultado = servicio.ConfirmarVenta(Venta.PagoEfectivo, 20m);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(7, Existencia(idProducto));

        var guardada = new VentaRepository().ObtenerPorId(resultado.VentaRegistrada!.IdVenta)!;
        Assert.Equal(10.50m, guardada.Total);
        Assert.Equal(20m, guardada.MontoRecibido);
        Assert.Equal(9.50m, guardada.Cambio);
        Assert.Equal(caja.IdCaja, guardada.IdCaja);
        var linea = Assert.Single(guardada.Detalles);
        Assert.Equal(3, linea.Cantidad);
        Assert.Equal(3.50m, linea.PrecioUnitario);
    }

    [Fact]
    public void EfectivoInsuficiente_SeRechazaSinGuardarNada()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 2);

        var resultado = servicio.ConfirmarVenta(Venta.PagoEfectivo, 5m);

        Assert.False(resultado.Exitoso);
        Assert.Equal(10, Existencia(idProducto));
        Assert.Equal(0, Contar("Ventas"));
    }

    [Fact]
    public void PagoConTarjeta_NoRegistraMontoRecibidoNiCambio()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Crema", 2.25m, 4);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 1);

        var resultado = servicio.ConfirmarVenta(Venta.PagoTarjeta, 100m);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var guardada = new VentaRepository().ObtenerPorId(resultado.VentaRegistrada!.IdVenta)!;
        Assert.Equal(Venta.PagoTarjeta, guardada.MetodoPago);
        Assert.Null(guardada.MontoRecibido);
        Assert.Null(guardada.Cambio);
    }

    [Fact]
    public void AgregarMasDeLoDisponible_SeRechaza()
    {
        var idProducto = CrearProducto("Quesillo", 4.00m, 3);
        var servicio = new VentaService();

        Assert.True(servicio.AgregarProducto(idProducto, 2).Exitoso);
        var resultado = servicio.AgregarProducto(idProducto, 2); // 2 + 2 > 3

        Assert.False(resultado.Exitoso);
        Assert.Equal(2, Assert.Single(servicio.Carrito).Cantidad);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CantidadCeroONegativa_SeRechaza(int cantidad)
    {
        var idProducto = CrearProducto("Quesillo", 4.00m, 3);

        Assert.False(new VentaService().AgregarProducto(idProducto, cantidad).Exitoso);
    }

    [Fact]
    public void ProductoInactivo_NoSePuedeVender()
    {
        var idProducto = CrearProducto("Requesón", 1.50m, 5);
        new ProductoService().CambiarEstado(idProducto, false);

        Assert.False(new VentaService().AgregarProducto(idProducto, 1).Exitoso);
    }

    [Fact]
    public void VenderLaUltimaUnidad_DejaExistenciaEnCeroYBloqueaLaSiguiente()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso duro", 5.00m, 1);

        Assert.True(Vender(idProducto, 1).Exitoso);
        Assert.Equal(0, Existencia(idProducto));

        Assert.False(Vender(idProducto, 1).Exitoso);
        Assert.Equal(0, Existencia(idProducto));
        Assert.Equal(1, Contar("Ventas"));
    }

    /// <summary>
    /// Dos ventanas de venta con la última unidad en el carrito: solo la
    /// primera en confirmar la vende; la segunda falla dentro de la transacción.
    /// </summary>
    [Fact]
    public void DosCarritosConLaUltimaUnidad_SoloUnoLograVenderla()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso duro", 5.00m, 1);
        var primera = new VentaService();
        var segunda = new VentaService();
        primera.AgregarProducto(idProducto, 1);
        segunda.AgregarProducto(idProducto, 1);

        Assert.True(primera.ConfirmarVenta(Venta.PagoEfectivo, 5m).Exitoso);
        var resultado = segunda.ConfirmarVenta(Venta.PagoEfectivo, 5m);

        Assert.False(resultado.Exitoso);
        Assert.Contains("Existencia insuficiente", resultado.Mensaje);
        Assert.Equal(0, Existencia(idProducto));
        Assert.Equal(1, Contar("Ventas"));
    }

    /// <summary>
    /// Si la segunda línea falla al guardar, la primera tampoco debe
    /// descontarse: no queda cabecera, ni detalle, ni inventario modificado.
    /// </summary>
    [Fact]
    public void FalloEnUnaLinea_RevierteLaVentaCompleta()
    {
        AbrirCaja();
        var idA = CrearProducto("Queso fresco", 3.50m, 5);
        var idB = CrearProducto("Crema", 2.00m, 5);
        var servicio = new VentaService();
        servicio.AgregarProducto(idA, 2);
        servicio.AgregarProducto(idB, 3);

        // Otra operación deja menos crema de la que hay en el carrito.
        Ejecutar($"UPDATE Productos SET Existencia = 1000 WHERE IdProducto = {idB};"); // 1 unidad (milésimas)

        var resultado = servicio.ConfirmarVenta(Venta.PagoEfectivo, 50m);

        Assert.False(resultado.Exitoso);
        Assert.Equal(5, Existencia(idA));
        Assert.Equal(1, Existencia(idB));
        Assert.Equal(0, Contar("Ventas"));
        Assert.Equal(0, Contar("DetalleVentas"));
    }

    [Fact]
    public void ConfirmarDosVeces_RegistraUnaSolaVenta()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 2);

        var primera = servicio.ConfirmarVenta(Venta.PagoEfectivo, 10m);
        var segunda = servicio.ConfirmarVenta(Venta.PagoEfectivo, 10m);

        Assert.True(primera.Exitoso, primera.Mensaje);
        Assert.False(segunda.Exitoso);
        Assert.Equal(1, Contar("Ventas"));
        Assert.Equal(8, Existencia(idProducto));
    }

    [Fact]
    public void VentasConsecutivas_MantienenInventarioYTotalesExactos()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Yogur", 0.35m, 30);

        for (var i = 0; i < 20; i++)
        {
            Assert.True(Vender(idProducto, 1).Exitoso);
        }

        Assert.Equal(10, Existencia(idProducto));
        Assert.Equal(20, Contar("Ventas"));
        var totalVendido = new VentaRepository().ObtenerPorRango(null, null).Sum(v => v.Total);
        Assert.Equal(7.00m, totalVendido);
    }

    [Fact]
    public void CambiarElPrecioDespues_NoAlteraLasVentasYaRegistradas()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var venta = Vender(idProducto, 2).VentaRegistrada!;

        var producto = new ProductoRepository().ObtenerPorId(idProducto)!;
        producto.PrecioVenta = 4.25m;
        producto.Nombre = "Queso fresco especial";
        new ProductoService().Modificar(producto);

        var guardada = new VentaRepository().ObtenerPorId(venta.IdVenta)!;
        Assert.Equal(7.00m, guardada.Total);
        Assert.Equal(3.50m, guardada.Detalles[0].PrecioUnitario);
        Assert.Equal("Queso fresco", guardada.Detalles[0].NombreProducto);
    }
}
