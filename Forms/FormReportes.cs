using LacteosMadeline.Services;

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
        Load += FormReportes_Load;
    }

    private void FormReportes_Load(object? sender, EventArgs e)
    {
        dtpDesde.Value = DateTime.Today.AddDays(-30);
        dtpHasta.Value = DateTime.Today;
        GenerarReporteActivo();
    }

    private void btnGenerar_Click(object? sender, EventArgs e)
    {
        GenerarReporteActivo();
    }

    private void GenerarReporteActivo()
    {
        switch (tabReportes.SelectedIndex)
        {
            case 0: GenerarVentas(); break;
            case 1: GenerarCompras(); break;
            case 2: GenerarInventario(); break;
            case 3: GenerarBajoStock(); break;
            case 4: GenerarCaja(); break;
        }
    }

    private void GenerarVentas()
    {
        var datos = _reporteService.ReporteVentas(dtpDesde.Value.Date, dtpHasta.Value.Date);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        AjustarColumnas("IdVenta", "# Venta", "FechaHora", "Fecha / Hora",
                        "Total", "Total", "Acumulado", "Acumulado");
    }

    private void GenerarCompras()
    {
        var datos = _reporteService.ReporteCompras(dtpDesde.Value.Date, dtpHasta.Value.Date);
        dgvReporte.DataSource = null;
        dgvReporte.DataSource = datos;

        OcultarColumna("Detalles");
        if (dgvReporte.Columns["IdCompra"] != null)
        {
            dgvReporte.Columns["IdCompra"].HeaderText = "# Compra";
            dgvReporte.Columns["FechaHora"].HeaderText = "Fecha / Hora";
            dgvReporte.Columns["IdProveedor"].Visible = false;
            dgvReporte.Columns["NombreProveedor"].HeaderText = "Proveedor";
            dgvReporte.Columns["Total"].HeaderText = "Total";
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
            dgvReporte.Columns["Nombre"].HeaderText = "Producto";
            dgvReporte.Columns["NombreCategoria"].HeaderText = "Categoría";
            dgvReporte.Columns["PrecioCompra"].HeaderText = "P. Compra";
            dgvReporte.Columns["PrecioVenta"].HeaderText = "P. Venta";
            dgvReporte.Columns["Existencia"].HeaderText = "Existencia";
            dgvReporte.Columns["StockMinimo"].HeaderText = "Stock Mín.";
            dgvReporte.Columns["Estado"].HeaderText = "Activo";
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
            dgvReporte.Columns["Nombre"].HeaderText = "Producto";
            dgvReporte.Columns["NombreCategoria"].HeaderText = "Categoría";
            dgvReporte.Columns["PrecioVenta"].HeaderText = "P. Venta";
            dgvReporte.Columns["Existencia"].HeaderText = "Existencia";
            dgvReporte.Columns["StockMinimo"].HeaderText = "Stock Mín.";
            dgvReporte.Columns["PrecioCompra"].Visible = false;
            dgvReporte.Columns["Estado"].Visible = false;
        }
    }

    private void GenerarCaja()
    {
        // Mostrar el historial de todas las cajas en el grid
        var cajas = new Repositories.CajaRepository().ObtenerHistorial();

        dgvReporte.DataSource = null;
        dgvReporte.DataSource = cajas;

        OcultarColumna("Movimientos");
        if (dgvReporte.Columns.Count > 0 && dgvReporte.Columns["IdCaja"] != null)
        {
            dgvReporte.Columns["IdCaja"].HeaderText = "# Caja";
            dgvReporte.Columns["FechaApertura"].HeaderText = "Apertura";
            dgvReporte.Columns["MontoInicial"].HeaderText = "Monto Inicial";
            dgvReporte.Columns["FechaCierre"].HeaderText = "Cierre";
            dgvReporte.Columns["MontoFinal"].HeaderText = "Monto Final";
            dgvReporte.Columns["Estado"].HeaderText = "Estado";
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
                    $"Ventas: {resumen.TotalVentas:C2}  |  " +
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

    private void AjustarColumnas(params string[] pares)
    {
        for (int i = 0; i < pares.Length - 1; i += 2)
        {
            if (dgvReporte.Columns[pares[i]] != null)
            {
                dgvReporte.Columns[pares[i]].HeaderText = pares[i + 1];
            }
        }
    }

    private void OcultarColumna(string nombre)
    {
        if (dgvReporte.Columns[nombre] != null)
        {
            dgvReporte.Columns[nombre].Visible = false;
        }
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
