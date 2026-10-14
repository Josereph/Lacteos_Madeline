using System.IO.Compression;
using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Costo promedio de las compras, utilidad, configuración del negocio,
/// copia externa y restauración de respaldos, y exportación a Excel.
/// </summary>
public class CostosConfiguracionYRespaldosTests : PruebaConBaseDatos
{
    private static void Comprar(int idProducto, decimal cantidad, decimal costo)
    {
        var servicio = new CompraService();
        Assert.True(servicio.AgregarProducto(idProducto, cantidad, costo).Exitoso);
        var resultado = servicio.ConfirmarCompra(CrearProveedor());
        Assert.True(resultado.Exitoso, resultado.Mensaje);
    }

    [Fact]
    public void Compra_ActualizaElCostoConPromedioPonderado()
    {
        var idProducto = CrearProducto("Queso fresco", 5m, 10, precioCompra: 2.00m);

        Comprar(idProducto, 30, 3.00m); // (10 × 2 + 30 × 3) / 40 = 2.75

        var producto = new ProductoService().ObtenerPorId(idProducto)!;
        Assert.Equal(40, producto.Existencia);
        Assert.Equal(2.75m, producto.PrecioCompra);
    }

    [Fact]
    public void CompraSinExistenciaNiCosto_TomaElCostoDeLaCompra()
    {
        var idProducto = CrearProducto("Crema", 4m, 0, precioCompra: null, unidad: UnidadMedida.Litro);

        Comprar(idProducto, 2.5m, 1.80m);

        Assert.Equal(1.80m, new ProductoService().ObtenerPorId(idProducto)!.PrecioCompra);
    }

    [Fact]
    public void Utilidad_UsaElCostoVigenteAlMomentoDeVender()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 5m, 10, precioCompra: 3m);
        Vender(idProducto, 2); // vende 10, costo 6

        Comprar(idProducto, 8, 4.50m); // el costo sube después de la venta

        var fila = Assert.Single(new ReporteService().ReporteUtilidad());
        Assert.Equal(10m, fila.Ventas);
        Assert.Equal(6m, fila.Costo);
        Assert.Equal(4m, fila.Utilidad);
        Assert.Equal(40m, fila.MargenPorcentaje);
        Assert.False(fila.CostoIncompleto);
    }

    [Fact]
    public void ProductoSinCosto_SeMarcaComoCostoIncompleto()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Regalo", 1m, 5, precioCompra: null);
        Vender(idProducto, 1);

        Assert.True(Assert.Single(new ReporteService().ReporteUtilidad()).CostoIncompleto);
    }

    [Fact]
    public void DatosDelNegocio_SeGuardanYAparecenEnElTicket()
    {
        var resultado = new ConfiguracionService().GuardarDatosNegocio(
            "Lácteos La Vaquita", "Calle 1, San Miguel", "2222-3333", "0614-010101-101-1", "Vuelva pronto", 42);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        ConfiguracionNegocio.Aplicar(new Dictionary<string, string>()); // simula reiniciar el programa
        new ConfiguracionService().Cargar();

        Assert.Equal(42, ConfiguracionNegocio.AnchoTicket);
        var ticket = TicketHelper.GenerarTexto(new Venta { IdVenta = 1, Total = 1m });
        Assert.Contains("Lácteos La Vaquita", ticket);
        Assert.Contains("2222-3333", ticket);
        Assert.Contains("0614-010101-101-1", ticket);
        Assert.Contains("Vuelva pronto", ticket);
    }

    [Fact]
    public void ConfiguracionInvalida_SeRechaza()
    {
        var servicio = new ConfiguracionService();

        Assert.False(servicio.GuardarDatosNegocio(" ", null, null, null, null, 32).Exitoso);
        Assert.False(servicio.GuardarDatosNegocio("Negocio", null, null, null, null, 50).Exitoso);
        Assert.False(servicio.GuardarCarpetaRespaldoExterna(Path.Combine(Carpeta, "no-existe")).Exitoso);
        Assert.False(servicio.GuardarCarpetaRespaldoExterna(DatabaseConnection.DatabaseFolder).Exitoso);
    }

    [Fact]
    public void RespaldoAutomatico_SeCopiaTambienALaCarpetaExterna()
    {
        var usb = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "LacteosMadeline.Tests.USB", Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            Assert.True(new ConfiguracionService().GuardarCarpetaRespaldoExterna(usb).Exitoso);

            var resultado = BackupHelper.RealizarBackupAutomatico("prueba");

            Assert.True(resultado.Exitoso, resultado.Detalle);
            Assert.Contains("Copia externa", resultado.Detalle);
            Assert.Single(Directory.GetFiles(usb, "*.db"));
        }
        finally
        {
            Directory.Delete(usb, recursive: true);
        }
    }

    [Fact]
    public void Restaurar_DevuelveLosDatosDelRespaldoYGuardaLosActuales()
    {
        var idProducto = CrearProducto("Queso fresco", 3m, 10);
        var respaldo = Path.Combine(Carpeta, "externo", "respaldo.db");
        Assert.True(BackupHelper.RealizarBackup(respaldo).Exitoso);

        // Después del respaldo, alguien borra el inventario por error.
        new InventarioService().AjustarExistencia(idProducto, 0);
        CrearProducto("Producto nuevo", 1m, 1);

        var resultado = BackupHelper.RestaurarDesde(respaldo);

        Assert.True(resultado.Exitoso, resultado.Detalle);
        Assert.Equal(10, Existencia(idProducto));
        Assert.Equal(1, Contar("Productos"));
        Assert.Contains(Directory.GetFiles(BackupHelper.CarpetaRespaldosAutomaticos, "*.db"), f => f.Contains("antes-de-restaurar"));
    }

    [Fact]
    public void RestaurarArchivoQueNoEsRespaldo_SeRechazaSinTocarNada()
    {
        CrearProducto("Queso fresco", 3m, 10);
        var falso = Path.Combine(Carpeta, "falso.db");
        File.WriteAllText(falso, "esto no es una base de datos");

        Assert.False(BackupHelper.RestaurarDesde(falso).Exitoso);
        Assert.False(BackupHelper.RestaurarDesde(Path.Combine(Carpeta, "no-existe.db")).Exitoso);
        Assert.Equal(1, Contar("Productos"));
    }

    [Fact]
    public void ExportarAExcel_GeneraUnLibroValidoConNumerosYTextos()
    {
        var ruta = Path.Combine(Carpeta, "reporte.xlsx");

        var resultado = ExportadorExcel.Exportar(ruta, "Ventas <prueba>",
            new[] { "Producto", "Cantidad", "Total" },
            new List<IReadOnlyList<object?>>
            {
                new object?[] { "Queso & crema", 1.25m, 4.38m },
                new object?[] { "Leche", 2, 2.50m }
            },
            new[] { false, false, true });

        Assert.True(resultado.Exitoso, resultado.Detalle);
        using var zip = ZipFile.OpenRead(ruta);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        using var hoja = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = hoja.ReadToEnd();
        Assert.Contains("Queso &amp; crema", xml);
        Assert.Contains("Ventas &lt;prueba&gt;", xml);
        Assert.Contains("<v>1.25</v>", xml);
        Assert.Contains("<v>4.38</v>", xml);
        System.Xml.Linq.XDocument.Parse(xml); // XML bien formado
    }

    [Fact]
    public void CorteDeCaja_IncluyeArqueoAnulacionesYDescuentos()
    {
        var caja = AbrirCaja(10m);
        var idProducto = CrearProducto("Queso fresco", 5m, 10);
        var servicio = new VentaService();
        servicio.AgregarProducto(idProducto, 2);
        servicio.AplicarDescuento(1m);
        servicio.ConfirmarVenta(Venta.PagoEfectivo, 9m);
        var anulada = Vender(idProducto, 1).VentaRegistrada!;
        new VentaService().Anular(anulada.IdVenta, "Error al cobrar");
        new CajaService().CerrarCaja(caja.IdCaja, 19m);

        var texto = TicketHelper.GenerarCorteCaja(new CajaService().ObtenerResumen(caja.IdCaja));

        Assert.Contains("CORTE DE CAJA", texto);
        Assert.Contains("$19.00", texto);       // esperado y contado
        Assert.Contains("$1.00", texto);        // descuentos
        Assert.Matches(@"Anuladas:\s+1", texto);
        Assert.All(texto.Split(Environment.NewLine), l => Assert.True(l.Length <= ConfiguracionNegocio.AnchoTicket, l));
    }

    [Fact]
    public void VentaNueva_GuardaElCostoDelMomentoYLasTablasNuevasExisten()
    {
        AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 5m, 10, precioCompra: 2.40m);
        var venta = Vender(idProducto, 1).VentaRegistrada!;

        Assert.Equal(240, Escalar<long>($"SELECT CostoUnitario FROM DetalleVentas WHERE IdVenta = {venta.IdVenta};"));
        foreach (var tabla in new[] { "Usuarios", "Clientes", "Configuracion" })
        {
            Assert.Equal(1, Escalar<long>($"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{tabla}';"));
        }
    }
}
