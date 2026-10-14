using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Venta por peso (cantidades con decimales), búsqueda por código de
/// barras, descuentos y clientes en el punto de venta.
/// </summary>
public class VentaPorPesoYDescuentosTests : PruebaConBaseDatos
{
    [Fact]
    public void VentaPorLibra_DescuentaExistenciaConDecimalesYRedondeaElSubtotal()
    {
        AbrirCaja();
        var idQueso = CrearProducto("Queso duro", 3.50m, 10m, unidad: UnidadMedida.Libra);

        var servicio = new VentaService();
        Assert.True(servicio.AgregarProducto(idQueso, 1.25m).Exitoso);
        var resultado = servicio.ConfirmarVenta(Venta.PagoEfectivo, 5m);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(8.75m, Existencia(idQueso));
        Assert.Equal(4.38m, resultado.VentaRegistrada!.Total); // 1.25 × 3.50 = 4.375 → 4.38
        Assert.Equal(8750, Escalar<long>($"SELECT Existencia FROM Productos WHERE IdProducto = {idQueso};"));

        var guardada = new VentaRepository().ObtenerPorId(resultado.VentaRegistrada.IdVenta)!;
        var linea = Assert.Single(guardada.Detalles);
        Assert.Equal(1.25m, linea.Cantidad);
        Assert.Equal(UnidadMedida.Libra, linea.Unidad);
    }

    [Fact]
    public void ProductoPorUnidad_NoAceptaCantidadesConDecimales()
    {
        AbrirCaja();
        var idCrema = CrearProducto("Crema", 2.00m, 10);

        var resultado = new VentaService().AgregarProducto(idCrema, 1.5m);

        Assert.False(resultado.Exitoso);
        Assert.Contains("entero", resultado.Mensaje);
    }

    [Fact]
    public void CantidadConMasDeTresDecimales_SeRechaza()
    {
        var idQueso = CrearProducto("Queso duro", 3.50m, 10m, unidad: UnidadMedida.Kilogramo);

        Assert.False(new VentaService().AgregarProducto(idQueso, 1.2345m).Exitoso);
    }

    [Fact]
    public void CodigoDeBarras_AgregaElProductoYCodigoDuplicadoSeRechaza()
    {
        var idLeche = CrearProducto("Leche entera", 1.25m, 10, codigo: "7401234567890");

        var servicio = new VentaService();
        var agregado = servicio.AgregarPorCodigo(" 7401234567890 ");
        Assert.True(agregado.Exitoso, agregado.Mensaje);
        Assert.Equal(idLeche, Assert.Single(servicio.Carrito).IdProducto);

        Assert.False(servicio.AgregarPorCodigo("000").Exitoso);

        var duplicado = new ProductoService().Registrar(new Producto
        {
            Nombre = "Otra leche", IdCategoria = IdCategoriaOtros, PrecioVenta = 1m, Codigo = "7401234567890"
        });
        Assert.False(duplicado.Exitoso);
        Assert.Contains("ya pertenece", duplicado.Mensaje);
    }

    [Fact]
    public void CambiarAUnidadConExistenciaConDecimales_SeRechaza()
    {
        var idQueso = CrearProducto("Queso duro", 3.50m, 2.5m, unidad: UnidadMedida.Libra);
        var producto = new ProductoService().ObtenerPorId(idQueso)!;
        producto.Unidad = UnidadMedida.Unidad;

        var resultado = new ProductoService().Modificar(producto);

        Assert.False(resultado.Exitoso);
        Assert.Equal(UnidadMedida.Libra, new ProductoService().ObtenerPorId(idQueso)!.Unidad);
    }

    [Fact]
    public void Descuento_SeRestaDelTotalYDelEfectivoEsperado()
    {
        var caja = AbrirCaja(10m);
        var idProducto = CrearProducto("Queso fresco", 5.00m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 2); // 10.00

        Assert.True(servicio.AplicarDescuento(1.50m).Exitoso);
        var resultado = servicio.ConfirmarVenta(Venta.PagoEfectivo, 8.50m);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var venta = new VentaRepository().ObtenerPorId(resultado.VentaRegistrada!.IdVenta)!;
        Assert.Equal(8.50m, venta.Total);
        Assert.Equal(1.50m, venta.Descuento);
        Assert.Equal(10.00m, venta.Subtotal);

        var resumen = new CajaService().ObtenerResumen(caja.IdCaja);
        Assert.Equal(18.50m, resumen.SaldoEsperado);
        Assert.Equal(1.50m, resumen.TotalDescuentos);
        Assert.Contains("Descuento:", TicketHelper.GenerarTexto(venta));
    }

    [Fact]
    public void DescuentoPorcentaje_SeCalculaSobreElSubtotal()
    {
        var idProducto = CrearProducto("Queso fresco", 3.33m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 3); // 9.99

        Assert.True(servicio.AplicarDescuentoPorcentaje(10).Exitoso);

        Assert.Equal(1.00m, servicio.Descuento); // 0.999 → 1.00
        Assert.Equal(8.99m, servicio.Total);
    }

    [Fact]
    public void DescuentoMayorQueElSubtotal_SeRechazaYAlQuitarProductosSeAjusta()
    {
        var idA = CrearProducto("A", 5m, 10);
        var idB = CrearProducto("B", 5m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idA, 1);
        servicio.AgregarProducto(idB, 1);

        Assert.False(servicio.AplicarDescuento(10.01m).Exitoso);
        Assert.True(servicio.AplicarDescuento(8m).Exitoso);

        servicio.EliminarProducto(idB); // subtotal 5 < descuento 8

        Assert.Equal(0m, servicio.Descuento);
        Assert.Equal(5m, servicio.Total);
    }

    [Fact]
    public void Descuento_DeUnCajeroRequiereAutorizacion()
    {
        Sesion.Iniciar(CrearUsuario("Cajera", Usuario.RolCajero));
        var admin = CrearUsuario("Jefa", Usuario.RolAdministrador);
        var idProducto = CrearProducto("Queso fresco", 5m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 1);

        Assert.False(servicio.AplicarDescuento(1m).Exitoso);
        Assert.True(servicio.AplicarDescuento(1m, idAutoriza: admin.IdUsuario).Exitoso);
        Assert.Equal(admin.IdUsuario, servicio.IdUsuarioDescuento);
    }

    [Fact]
    public void VentaConCliente_GuardaElClienteYApareceEnElTicket()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3m, 10);
        var cliente = new ClienteService().Guardar(new Cliente { Nombre = "Tienda La Esquina" });
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 1);

        Assert.True(servicio.SeleccionarCliente(cliente.IdCliente).Exitoso);
        var resultado = servicio.ConfirmarVenta(Venta.PagoTarjeta, null);

        var venta = new VentaRepository().ObtenerPorId(resultado.VentaRegistrada!.IdVenta)!;
        Assert.Equal("Tienda La Esquina", venta.NombreCliente);
        Assert.Contains("Tienda La Esquina", TicketHelper.GenerarTexto(venta));
    }

    [Fact]
    public void ClienteInactivo_NoSePuedeSeleccionar()
    {
        var servicio = new ClienteService();
        var cliente = servicio.Guardar(new Cliente { Nombre = "Cliente viejo" });
        servicio.AlternarEstado(cliente.IdCliente);

        Assert.False(new VentaService().SeleccionarCliente(cliente.IdCliente).Exitoso);
    }

    [Fact]
    public void ClienteConCorreoInvalido_SeRechaza()
    {
        var resultado = new ClienteService().Guardar(new Cliente { Nombre = "Ana", Correo = "ana arroba correo" });

        Assert.False(resultado.Exitoso);
        Assert.Equal(0, Contar("Clientes"));
    }

    [Fact]
    public void Venta_RegistraQuienLaHizo()
    {
        var cajera = CrearUsuario("Cajera", Usuario.RolCajero);
        Sesion.Iniciar(cajera);
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3m, 10);

        var venta = Vender(idProducto, 1).VentaRegistrada!;

        Assert.Equal(cajera.IdUsuario, Escalar<long>($"SELECT IdUsuario FROM Ventas WHERE IdVenta = {venta.IdVenta};"));
        Assert.Equal("Cajera", new VentaRepository().ObtenerPorId(venta.IdVenta)!.NombreUsuario);
    }
}
