using LacteosMadeline.Forms;

namespace LacteosMadeline;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        Shown += Form1_Shown;
    }

    /// <summary>
    /// Alerta de bajo inventario al iniciar la jornada.
    /// </summary>
    private void Form1_Shown(object? sender, EventArgs e)
    {
        var bajoStock = new LacteosMadeline.Services.InventarioService().ConsultarBajoStock();
        if (bajoStock.Count == 0) return;

        var lista = string.Join("\n", bajoStock.Take(15)
            .Select(p => $"• {p.Nombre}: {p.Existencia} (mínimo {p.StockMinimo})"));
        if (bajoStock.Count > 15) lista += $"\n… y {bajoStock.Count - 15} más (ver Reportes › Bajo Stock).";

        MessageBox.Show(
            $"Hay {bajoStock.Count} producto(s) con existencia igual o menor al stock mínimo:\n\n{lista}",
            "Alerta de inventario bajo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    // ── Fase 1 ──────────────────────────────────────────────────────────

    private void btnCategorias_Click(object? sender, EventArgs e)
    {
        using var form = new FormCategorias();
        form.ShowDialog(this);
    }

    private void btnProductos_Click(object? sender, EventArgs e)
    {
        using var form = new FormProductos();
        form.ShowDialog(this);
    }

    private void btnVentas_Click(object? sender, EventArgs e)
    {
        using var form = new FormVentas();
        form.ShowDialog(this);
    }

    // ── Fase 2 ──────────────────────────────────────────────────────────

    private void btnProveedores_Click(object? sender, EventArgs e)
    {
        using var form = new FormProveedores();
        form.ShowDialog(this);
    }

    private void btnCompras_Click(object? sender, EventArgs e)
    {
        using var form = new FormCompras();
        form.ShowDialog(this);
    }

    private void btnCaja_Click(object? sender, EventArgs e)
    {
        using var form = new FormCaja();
        form.ShowDialog(this);
    }

    private void btnConsultaVentas_Click(object? sender, EventArgs e)
    {
        using var form = new FormConsultaVentas();
        form.ShowDialog(this);
    }

    private void btnConsultaCompras_Click(object? sender, EventArgs e)
    {
        using var form = new FormConsultaCompras();
        form.ShowDialog(this);
    }

    private void btnReportes_Click(object? sender, EventArgs e)
    {
        using var form = new FormReportes();
        form.ShowDialog(this);
    }

    private void btnBackup_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Guardar respaldo de base de datos",
            Filter = "Base de datos SQLite (*.db)|*.db",
            FileName = LacteosMadeline.Helpers.BackupHelper.GenerarNombreArchivo()
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var (exitoso, detalle) = LacteosMadeline.Helpers.BackupHelper.RealizarBackup(dlg.FileName);

        MessageBox.Show(detalle,
            exitoso ? "Respaldo exitoso" : "Error en respaldo",
            MessageBoxButtons.OK,
            exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private void btnSalir_Click(object? sender, EventArgs e)
    {
        Close();
    }
}
