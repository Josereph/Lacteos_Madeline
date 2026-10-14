using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Edición de productos y ajustes de inventario: modificar un producto
/// nunca pisa la existencia, y cada ajuste queda registrado.
/// </summary>
public class ProductosTests : PruebaConBaseDatos
{
    private static Producto Obtener(int idProducto) => new ProductoService().ObtenerPorId(idProducto)!;

    [Fact]
    public void ModificarConExistenciaVieja_NoBorraLaVentaHechaMientrasSeEditaba()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        var enPantalla = Obtener(idProducto); // el usuario abre el producto (existencia 10)

        Assert.True(Vender(idProducto, 3).Exitoso); // mientras tanto se venden 3

        enPantalla.PrecioVenta = 4.00m;
        var resultado = new ProductoService().Modificar(enPantalla); // guarda con Existencia = 10

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(7, Existencia(idProducto));
        Assert.Equal(4.00m, Obtener(idProducto).PrecioVenta);
    }

    [Fact]
    public void Ajuste_QuedaRegistradoConValorAnteriorNuevoYMotivo()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);

        var resultado = new InventarioService().AjustarExistencia(idProducto, 8, "Conteo del lunes");

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(8, Existencia(idProducto));
        Assert.Equal(1, Contar("AjustesInventario"));
        Assert.Equal(10, Escalar<long>("SELECT ExistenciaAnterior FROM AjustesInventario;"));
        Assert.Equal(8, Escalar<long>("SELECT ExistenciaNueva FROM AjustesInventario;"));
        Assert.Equal("Conteo del lunes", Escalar<string>("SELECT Motivo FROM AjustesInventario;"));
        Assert.Equal("Queso fresco", Escalar<string>("SELECT NombreProducto FROM AjustesInventario;"));
    }

    [Fact]
    public void AjusteSobreUnaExistenciaQueCambio_SeRechazaSinTocarNada()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        Assert.True(Vender(idProducto, 2).Exitoso); // la existencia real pasa a 8

        var resultado = new InventarioService().AjustarExistencia(
            idProducto, 15, InventarioService.MotivoCorreccionProducto, existenciaEsperada: 10);

        Assert.False(resultado.Exitoso);
        Assert.Contains("cambió", resultado.Mensaje);
        Assert.Equal(8, Existencia(idProducto));
        Assert.Equal(0, Contar("AjustesInventario"));
    }

    [Fact]
    public void AjusteAlMismoValor_NoGeneraRegistro()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);

        Assert.True(new InventarioService().AjustarExistencia(idProducto, 10).Exitoso);
        Assert.Equal(0, Contar("AjustesInventario"));
    }

    [Fact]
    public void AjusteDeProductoInexistente_SeRechaza()
    {
        var resultado = new InventarioService().AjustarExistencia(999, 5);

        Assert.False(resultado.Exitoso);
        Assert.Contains("no existe", resultado.Mensaje);
    }

    [Fact]
    public void PrecioConMasDeDosDecimales_SeRechaza()
    {
        var resultado = new ProductoService().Registrar(new Producto
        {
            Nombre = "Queso",
            IdCategoria = IdCategoriaOtros,
            PrecioVenta = 3.555m
        });

        Assert.False(resultado.Exitoso);
        Assert.Equal(0, Contar("Productos"));
    }

    [Fact]
    public void Precios_SeGuardanEnCentavosYSeLeenIguales()
    {
        var idProducto = CrearProducto("Crema", 2.35m, 1, precioCompra: 1.05m);

        Assert.Equal(235, Escalar<long>($"SELECT PrecioVenta FROM Productos WHERE IdProducto = {idProducto};"));
        Assert.Equal(2.35m, Obtener(idProducto).PrecioVenta);
        Assert.Equal(1.05m, Obtener(idProducto).PrecioCompra);
    }

    [Fact]
    public void Categorias_NoAdmitenNombresRepetidosNiVacios()
    {
        var servicio = new CategoriaService();

        Assert.True(servicio.Guardar(0, "Quesos").Exitoso);
        Assert.False(servicio.Guardar(0, "  quesos ").Exitoso);
        Assert.False(servicio.Guardar(0, "   ").Exitoso);
        Assert.Equal(2, Contar("Categorias")); // "Otros" + "Quesos"
    }

    [Fact]
    public void Categoria_SeDesactivaYSeReactiva()
    {
        var servicio = new CategoriaService();

        Assert.True(servicio.AlternarEstado(IdCategoriaOtros).Exitoso);
        Assert.Empty(servicio.ObtenerTodas(incluirInactivas: false));
        Assert.True(servicio.AlternarEstado(IdCategoriaOtros).Exitoso);
        Assert.Single(servicio.ObtenerTodas(incluirInactivas: false));
    }

    [Fact]
    public void Proveedor_SeGuardaSinEspaciosSobrantesYCampoVacioComoNulo()
    {
        var servicio = new ProveedorService();
        var proveedor = new Proveedor { Nombre = "  Lácteos del Valle ", Telefono = "   ", Contacto = " Ana " };

        Assert.True(servicio.Guardar(proveedor).Exitoso);

        var guardado = Assert.Single(servicio.Buscar());
        Assert.Equal("Lácteos del Valle", guardado.Nombre);
        Assert.Equal("Ana", guardado.Contacto);
        Assert.Null(guardado.Telefono);
    }

    [Fact]
    public void ProveedorSinNombre_SeRechaza()
    {
        Assert.False(new ProveedorService().Guardar(new Proveedor { Nombre = " " }).Exitoso);
        Assert.Equal(0, Contar("Proveedores"));
    }
}
