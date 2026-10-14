using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de reportes con TabControl: Ventas, Compras, Inventario,
/// Bajo Stock y Caja. Permite filtros de fecha y tiene opción de
/// imprimir el reporte visible (sección 17-19 de los lineamientos).
/// </summary>
public partial class FormReportes : Form
{
    private readonly ReporteService _reporteService = new();

    public FormReportes()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormReportes_Load;
    }

    private void FormReportes_Load(object? sender, EventArgs e)
    {
        dtpDesde.Value = DateTime.Today.AddDays(-30);
        dtpHasta.Value = DateTime.Today;
        GenerarReporteActivo();
    }

    public const int PestanaBajoStock = 3;

    /// <summary>
    /// Abre directamente una pestaña de reporte (lo usa la alerta de
    /// stock crítico de la barra superior).
    /// </summary>
    public void MostrarPestana(int indice)
    {
        if (indice < 0 || indice >= tabReportes.TabCount) return;

        if (tabReportes.SelectedIndex == indice)
        {
            GenerarReporteActivo();
        }
        else
        {
            tabReportes.SelectedIndex = indice; // genera el reporte vía SelectedIndexChanged
        }
    }

    private void btnGenerar_Click(object? sender, EventArgs e)
    {
        GenerarReporteActivo();
    }

    private void GenerarReporteActivo()
    {
        MostrarMensaje(string.Empty, esError: false);

        if (dtpDesde.Value.Date > dtpHasta.Value.Date)
        {
            MostrarMensaje("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.");
            return;
        }

        try
        {
            switch (tabReportes.SelectedIndex)
            {
                case 0: GenerarVentas(); break;
                case 1: GenerarCompras(); break;
                case 2: GenerarInventario(); break;
                case 3: GenerarBajoStock(); break;
                case 4: GenerarCaja(); break;
                case 5: GenerarMasVendidos(); break;
                case 6: GenerarUtilidad(); break;
                case 7: GenerarAjustes(); break;
            }
        }
        catch (Exception ex)
        {
            Helpers.LogHelper.Error($"Generar reporte '{tabReportes.SelectedTab?.Text}'", ex);
            dgvReporte.DataSource = null;
            MostrarMensaje($"No se pudo generar el reporte: {ex.Message}");
        }
    }

    private void GenerarVentas()
    {
        var datos = _reporteService.ReporteVentas(dtpDesde.Value.Date, dtpHasta.Value.Date);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        AjustarColumnas("IdVenta", "# Venta", "FechaHora", "Fecha / Hora", "Usuario", "Atendió",
                        "MetodoPago", "Método de pago",
                        "Total", "Total", "Acumulado", "Acumulado (sin anuladas)");
        FormatoMoneda("Descuento", "Total", "Acumulado");

        var activas = datos.Where(d => d.Estado == Models.Venta.EstadoActiva).ToList();
        var anuladas = datos.Count - activas.Count;
        MostrarMensaje(
            $"{activas.Count} venta(s)  |  Total: {activas.Sum(d => d.Total):C2}  |  " +
            $"Efectivo: {activas.Where(d => d.MetodoPago == Models.Venta.PagoEfectivo).Sum(d => d.Total):C2}  |  " +
            $"Descuentos: {activas.Sum(d => d.Descuento):C2}" +
            (anuladas > 0 ? $"  |  Anuladas: {anuladas} (no suman)" : string.Empty),
            esError: false);
    }

    private void GenerarMasVendidos()
    {
        var datos = _reporteService.ProductosMasVendidos(dtpDesde.Value.Date, dtpHasta.Value.Date);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        AjustarColumnas("Posicion", "#", "Producto", "Producto",
                        "UnidadesVendidas", "Cantidad vendida", "TotalVendido", "Total vendido",
                        "NumeroVentas", "N.º de ventas", "ExistenciaActual", "Existencia actual");
        FormatoMoneda("TotalVendido");
        dgvReporte.FormatoColumna("UnidadesVendidas", "0.###");
        dgvReporte.FormatoColumna("ExistenciaActual", "0.###");

        MostrarMensaje(datos.Count == 0
            ? "No hay ventas en el rango de fechas seleccionado."
            : $"{datos.Count} producto(s) vendidos en el período.", esError: false);
    }

    private void GenerarCompras()
    {
        var datos = _reporteService.ReporteCompras(dtpDesde.Value.Date, dtpHasta.Value.Date);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        OcultarColumna("Detalles");
        if (dgvReporte.Columns["IdCompra"] != null)
        {
            dgvReporte.Encabezado("IdCompra", "# Compra");
            dgvReporte.Encabezado("FechaHora", "Fecha / Hora");
            dgvReporte.OcultarColumna("IdProveedor");
            dgvReporte.Encabezado("NombreProveedor", "Proveedor");
            dgvReporte.Encabezado("Total", "Total");
            dgvReporte.Encabezado("NombreUsuario", "Registró");
            FormatoMoneda("Total");
        }
    }

    private void GenerarInventario()
    {
        var datos = _reporteService.ReporteInventario();
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        OcultarColumna("IdProducto");
        OcultarColumna("IdCategoria");
        if (dgvReporte.Columns["Nombre"] != null)
        {
            dgvReporte.Encabezado("Nombre", "Producto");
            dgvReporte.Encabezado("NombreCategoria", "Categoría");
            dgvReporte.Encabezado("PrecioCompra", "P. Compra");
            dgvReporte.Encabezado("PrecioVenta", "P. Venta");
            dgvReporte.Encabezado("Existencia", "Existencia");
            dgvReporte.Encabezado("StockMinimo", "Stock Mín.");
            dgvReporte.Encabezado("Estado", "Activo");
            dgvReporte.Encabezado("Codigo", "Código");
            dgvReporte.Encabezado("Unidad", "Se vende por");
            FormatoMoneda("PrecioCompra", "PrecioVenta");
            dgvReporte.FormatoColumna("Existencia", "0.###");
            dgvReporte.FormatoColumna("StockMinimo", "0.###");

            var valorInventario = datos.Sum(p => (p.PrecioCompra ?? 0) * p.Existencia);
            MostrarMensaje($"{datos.Count} producto(s) activos  |  Valor del inventario al costo: {valorInventario:C2}", esError: false);
        }
    }

    private void GenerarBajoStock()
    {
        var datos = _reporteService.ProductosBajoStock();
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        OcultarColumna("IdProducto");
        OcultarColumna("IdCategoria");
        if (dgvReporte.Columns["Nombre"] != null)
        {
            dgvReporte.Encabezado("Nombre", "Producto");
            dgvReporte.Encabezado("NombreCategoria", "Categoría");
            dgvReporte.Encabezado("PrecioVenta", "P. Venta");
            dgvReporte.Encabezado("Existencia", "Existencia");
            dgvReporte.Encabezado("StockMinimo", "Stock Mín.");
            dgvReporte.OcultarColumna("PrecioCompra");
            dgvReporte.OcultarColumna("Estado");
            dgvReporte.Encabezado("Codigo", "Código");
            dgvReporte.Encabezado("Unidad", "Se vende por");
            FormatoMoneda("PrecioVenta");
            dgvReporte.FormatoColumna("Existencia", "0.###");
            dgvReporte.FormatoColumna("StockMinimo", "0.###");
        }
    }

    private void GenerarCaja()
    {
        // Mostrar el historial de todas las cajas en el grid
        var cajas = new CajaService().ObtenerHistorial();

        dgvReporte.DataSource = null;
        dgvReporte.DataSource = cajas;

        OcultarColumna("Movimientos");
        if (dgvReporte.Columns.Count > 0 && dgvReporte.Columns["IdCaja"] != null)
        {
            dgvReporte.Encabezado("IdCaja", "# Caja");
            dgvReporte.Encabezado("FechaApertura", "Apertura");
            dgvReporte.Encabezado("MontoInicial", "Monto Inicial");
            dgvReporte.Encabezado("FechaCierre", "Cierre");
            dgvReporte.Encabezado("MontoFinal", "Monto Contado");
            dgvReporte.Encabezado("MontoEsperado", "Monto Esperado");
            dgvReporte.Encabezado("Diferencia", "Diferencia");
            dgvReporte.Encabezado("Estado", "Estado");
            dgvReporte.Encabezado("NombreUsuarioApertura", "Abrió");
            dgvReporte.Encabezado("NombreUsuarioCierre", "Cerró");
            FormatoMoneda("MontoInicial", "MontoFinal", "MontoEsperado", "Diferencia");
        }

        // Mostrar resumen de caja abierta si existe
        var cajaAbierta = new CajaService().ObtenerCajaAbierta();
        if (cajaAbierta is not null)
        {
            var resumen = _reporteService.ResumenCajaPorId(cajaAbierta.IdCaja);
            if (resumen is not null)
            {
                MostrarMensaje(
                    $"Caja #{cajaAbierta.IdCaja} ABIERTA  |  " +
                    $"Monto inicial: {resumen.Caja.MontoInicial:C2}  |  " +
                    $"Ventas efectivo: {resumen.VentasEfectivo:C2}  |  " +
                    $"Tarjeta/transf.: {resumen.VentasOtrosMetodos:C2}  |  " +
                    $"Entradas: {resumen.TotalEntradas:C2}  |  " +
                    $"Salidas: {resumen.TotalSalidas:C2}  |  " +
                    $"Saldo esperado: {resumen.SaldoEsperado:C2}",
                    esError: false);
                return;
            }
        }
        MostrarMensaje($"Se encontraron {cajas.Count} caja(s) registradas.", esError: false);
    }

    private void btnImprimir_Click(object? sender, EventArgs e)
    {
        // Impresión básica del contenido del grid
        using var pd = new System.Drawing.Printing.PrintDocument();
        pd.PrintPage += (s, ev) =>
        {
            if (ev.Graphics is null) return;
            using var fuente = new Font("Courier New", 8F);
            float y = ev.MarginBounds.Top;
            float x = ev.MarginBounds.Left;

            ev.Graphics.DrawString($"LÁCTEOS MADELINE – {tabReportes.SelectedTab?.Text ?? "Reporte"}",
                new Font("Courier New", 10F, FontStyle.Bold), System.Drawing.Brushes.Black, x, y);
            y += 20;
            ev.Graphics.DrawString($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                fuente, System.Drawing.Brushes.Black, x, y);
            y += 20;

            foreach (DataGridViewRow row in dgvReporte.Rows)
            {
                if (y > ev.MarginBounds.Bottom - 20) break;
                var linea = string.Join("  |  ",
                    row.Cells.Cast<DataGridViewCell>()
                       .Where(c => c.Visible)
                       .Select(c => c.FormattedValue?.ToString()?.PadRight(12)));
                ev.Graphics.DrawString(linea, fuente, System.Drawing.Brushes.Black, x, y);
                y += 14;
            }
        };

        var dlg = new PrintDialog { Document = pd };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            pd.Print();
        }
    }

    private void tabReportes_SelectedIndexChanged(object? sender, EventArgs e)
    {
        GenerarReporteActivo();
    }

    private void GenerarUtilidad()
    {
        var desde = dtpDesde.Value.Date;
        var hasta = dtpHasta.Value.Date;
        var datos = _reporteService.ReporteUtilidad(desde, hasta);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        AjustarColumnas("Unidad", "Unidad", "Cantidad", "Cantidad vendida", "Ventas", "Ventas",
                        "Costo", "Costo", "Utilidad", "Utilidad bruta", "MargenPorcentaje", "Margen %",
                        "CostoIncompleto", "Sin costo registrado");
        FormatoMoneda("Ventas", "Costo", "Utilidad");
        dgvReporte.FormatoColumna("Cantidad", "0.###");

        var ventas = datos.Sum(d => d.Ventas);
        var costo = datos.Sum(d => d.Costo);
        var descuentos = _reporteService.TotalDescuentos(desde, hasta);
        var neta = ventas - descuentos - costo;
        var incompletos = datos.Count(d => d.CostoIncompleto);

        MostrarMensaje(
            $"Ventas: {ventas:C2}  |  Descuentos: -{descuentos:C2}  |  Costo: -{costo:C2}  |  Utilidad: {neta:C2}" +
            (ventas > 0 ? $" ({Math.Round(neta * 100 / ventas, 1)} %)" : string.Empty) +
            (incompletos > 0 ? $"  |  {incompletos} producto(s) sin precio de compra: su utilidad aparece mayor" : string.Empty),
            esError: false);
    }

    private void GenerarAjustes()
    {
        var datos = _reporteService.ReporteAjustes(dtpDesde.Value.Date, dtpHasta.Value.Date);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        OcultarColumna("IdAjuste");
        AjustarColumnas("FechaHora", "Fecha / Hora", "NombreProducto", "Producto",
                        "ExistenciaAnterior", "Antes", "ExistenciaNueva", "Después",
                        "Diferencia", "Diferencia", "NombreUsuario", "Usuario");
        foreach (var columna in new[] { "ExistenciaAnterior", "ExistenciaNueva", "Diferencia" })
        {
            dgvReporte.FormatoColumna(columna, "0.###");
        }

        MostrarMensaje($"{datos.Count} ajuste(s) de inventario en el período.", esError: false);
    }

    private void FormatoMoneda(params string[] columnas)
    {
        foreach (var columna in columnas)
        {
            dgvReporte.FormatoColumna(columna, "C2");
        }
    }

    private void btnExportar_Click(object? sender, EventArgs e)
    {
        if (dgvReporte.Rows.Count == 0)
        {
            MostrarMensaje("Genere un reporte con datos antes de exportarlo.");
            return;
        }

        var titulo = $"{tabReportes.SelectedTab?.Text ?? "Reporte"} {dtpDesde.Value:dd-MM-yyyy} a {dtpHasta.Value:dd-MM-yyyy}";
        using var dialogo = new SaveFileDialog
        {
            Title = "Exportar reporte a Excel",
            Filter = "Libro de Excel (*.xlsx)|*.xlsx",
            FileName = $"{titulo.Replace('/', '-')}.xlsx"
        };

        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        var (exitoso, detalle) = Helpers.ExportadorExcel.Exportar(dgvReporte, dialogo.FileName, titulo);
        MostrarMensaje(detalle, esError: !exitoso);
    }

    private void AjustarColumnas(params string[] pares)
    {
        for (int i = 0; i < pares.Length - 1; i += 2)
        {
            dgvReporte.Encabezado(pares[i], pares[i + 1]);
        }
    }

    private void OcultarColumna(string nombre)
    {
        dgvReporte.OcultarColumna(nombre);
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        lblMensaje.Text = mensaje;
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        Diseno.Anclar(Diseno.ArribaAncho, tabReportes);
        Diseno.Anclar(Diseno.Todo, dgvReporte);
        Diseno.Anclar(Diseno.AbajoAncho, lblMensaje);
    }
}
