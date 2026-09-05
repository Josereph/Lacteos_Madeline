namespace LacteosMadeline.Forms;

partial class FormConsultaVentas
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
    private Label lblIdVenta;
    private NumericUpDown nudIdVenta;
    private Button btnBuscar;
    private DataGridView dgvVentas;
    private Label lblTotalVentas;
    private Button btnReimprimir;
    private Label lblDetalleTitle;
    private DataGridView dgvDetalle;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo      = new Label();
        lblDesde       = new Label();
        dtpDesde       = new DateTimePicker();
        lblHasta       = new Label();
        dtpHasta       = new DateTimePicker();
        lblIdVenta     = new Label();
        nudIdVenta     = new NumericUpDown();
        btnBuscar      = new Button();
        dgvVentas      = new DataGridView();
        lblTotalVentas = new Label();
        btnReimprimir  = new Button();
        lblDetalleTitle = new Label();
        dgvDetalle     = new DataGridView();

        SuspendLayout();

        // Título
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 12);
        lblTitulo.Text = "Consulta de Ventas";

        // Filtros
        lblDesde.Text = "Desde:";
        lblDesde.Location = new Point(12, 52);
        lblDesde.AutoSize = true;

        dtpDesde.Location = new Point(62, 48);
        dtpDesde.Size = new Size(140, 23);
        dtpDesde.Format = DateTimePickerFormat.Short;

        lblHasta.Text = "Hasta:";
        lblHasta.Location = new Point(215, 52);
        lblHasta.AutoSize = true;

        dtpHasta.Location = new Point(260, 48);
        dtpHasta.Size = new Size(140, 23);
        dtpHasta.Format = DateTimePickerFormat.Short;

        lblIdVenta.Text = "# Venta:";
        lblIdVenta.Location = new Point(415, 52);
        lblIdVenta.AutoSize = true;

        nudIdVenta.Location = new Point(468, 48);
        nudIdVenta.Size = new Size(80, 23);
        nudIdVenta.Minimum = 0;
        nudIdVenta.Maximum = 9999999;
        nudIdVenta.Value = 0;

        btnBuscar.Location = new Point(558, 46);
        btnBuscar.Size = new Size(80, 27);
        btnBuscar.Text = "Buscar";
        btnBuscar.Click += btnBuscar_Click;

        // Grid ventas
        dgvVentas.Location = new Point(12, 85);
        dgvVentas.Size = new Size(680, 200);
        dgvVentas.ReadOnly = true;
        dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvVentas.MultiSelect = false;
        dgvVentas.AllowUserToAddRows = false;
        dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvVentas.SelectionChanged += dgvVentas_SelectionChanged;

        // Resumen + botón reimprimir
        lblTotalVentas.Location = new Point(12, 293);
        lblTotalVentas.Size = new Size(500, 18);
        lblTotalVentas.AutoSize = false;
        lblTotalVentas.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblTotalVentas.ForeColor = Color.DarkSlateBlue;

        btnReimprimir.Location = new Point(560, 290);
        btnReimprimir.Size = new Size(132, 25);
        btnReimprimir.Text = "Ver / Reimprimir ticket";
        btnReimprimir.Click += btnReimprimir_Click;

        // Detalle
        lblDetalleTitle.Text = "Detalle de la venta seleccionada:";
        lblDetalleTitle.Location = new Point(12, 324);
        lblDetalleTitle.AutoSize = true;
        lblDetalleTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        dgvDetalle.Location = new Point(12, 344);
        dgvDetalle.Size = new Size(680, 190);
        dgvDetalle.ReadOnly = true;
        dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDetalle.AllowUserToAddRows = false;
        dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Form
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(706, 550);
        Text = "Lácteos Madeline – Consulta de Ventas";
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            lblTitulo,
            lblDesde, dtpDesde, lblHasta, dtpHasta, lblIdVenta, nudIdVenta, btnBuscar,
            dgvVentas, lblTotalVentas, btnReimprimir, lblDetalleTitle, dgvDetalle
        });

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
