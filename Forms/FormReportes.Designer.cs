namespace LacteosMadeline.Forms;

partial class FormReportes
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private Label lblTitulo;
    private Label lblDesde;
    private DateTimePicker dtpDesde;
    private Label lblHasta;
    private DateTimePicker dtpHasta;
    private Button btnGenerar;
    private Button btnImprimir;
    private TabControl tabReportes;
    private TabPage tabVentas;
    private TabPage tabCompras;
    private TabPage tabInventario;
    private TabPage tabBajoStock;
    private TabPage tabCaja;
    private DataGridView dgvReporte;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo    = new Label();
        lblDesde     = new Label();
        dtpDesde     = new DateTimePicker();
        lblHasta     = new Label();
        dtpHasta     = new DateTimePicker();
        btnGenerar   = new Button();
        btnImprimir  = new Button();
        tabReportes  = new TabControl();
        tabVentas    = new TabPage();
        tabCompras   = new TabPage();
        tabInventario = new TabPage();
        tabBajoStock = new TabPage();
        tabCaja      = new TabPage();
        dgvReporte   = new DataGridView();
        lblMensaje   = new Label();

        SuspendLayout();

        // Título
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 12);
        lblTitulo.Text = "Reportes";

        // Filtros fecha
        lblDesde.Text = "Desde:";
        lblDesde.Location = new Point(12, 52);
        lblDesde.AutoSize = true;

        dtpDesde.Location = new Point(62, 48);
        dtpDesde.Size = new Size(130, 23);
        dtpDesde.Format = DateTimePickerFormat.Short;

        lblHasta.Text = "Hasta:";
        lblHasta.Location = new Point(205, 52);
        lblHasta.AutoSize = true;

        dtpHasta.Location = new Point(250, 48);
        dtpHasta.Size = new Size(130, 23);
        dtpHasta.Format = DateTimePickerFormat.Short;

        btnGenerar.Location = new Point(392, 46);
        btnGenerar.Size = new Size(90, 27);
        btnGenerar.Text = "Generar";
        btnGenerar.Click += btnGenerar_Click;

        btnImprimir.Location = new Point(492, 46);
        btnImprimir.Size = new Size(90, 27);
        btnImprimir.Text = "Imprimir";
        btnImprimir.Click += btnImprimir_Click;

        // TabControl
        tabVentas.Text    = "Ventas";
        tabCompras.Text   = "Compras";
        tabInventario.Text = "Inventario";
        tabBajoStock.Text = "Bajo Stock";
        tabCaja.Text      = "Caja";

        tabReportes.Location = new Point(12, 85);
        tabReportes.Size = new Size(760, 35);
        tabReportes.TabPages.AddRange(new TabPage[] { tabVentas, tabCompras, tabInventario, tabBajoStock, tabCaja });
        tabReportes.SelectedIndexChanged += tabReportes_SelectedIndexChanged;

        // DataGridView — fuera del TabControl para mantener acceso directo
        dgvReporte.Location = new Point(12, 125);
        dgvReporte.Size = new Size(760, 380);
        dgvReporte.ReadOnly = true;
        dgvReporte.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvReporte.AllowUserToAddRows = false;
        dgvReporte.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        lblMensaje.Location = new Point(12, 515);
        lblMensaje.Size = new Size(760, 20);
        lblMensaje.AutoSize = false;

        // Form
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(790, 545);
        Text = "Lácteos Madeline – Reportes";
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            lblTitulo,
            lblDesde, dtpDesde, lblHasta, dtpHasta,
            btnGenerar, btnImprimir,
            tabReportes, dgvReporte, lblMensaje
        });

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
