namespace LacteosMadeline.Forms;

partial class FormConsultaCompras
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
    private Label lblProveedorFiltro;
    private ComboBox cboProveedor;
    private Button btnBuscar;
    private DataGridView dgvCompras;
    private Label lblTotalCompras;
    private Label lblDetalleTitle;
    private DataGridView dgvDetalle;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo        = new Label();
        lblDesde         = new Label();
        dtpDesde         = new DateTimePicker();
        lblHasta         = new Label();
        dtpHasta         = new DateTimePicker();
        lblProveedorFiltro = new Label();
        cboProveedor     = new ComboBox();
        btnBuscar        = new Button();
        dgvCompras       = new DataGridView();
        lblTotalCompras  = new Label();
        lblDetalleTitle  = new Label();
        dgvDetalle       = new DataGridView();

        SuspendLayout();

        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 12);
        lblTitulo.Text = "Consulta de Compras";

        lblDesde.Text = "Desde:";
        lblDesde.Location = new Point(12, 52);
        lblDesde.AutoSize = true;

        dtpDesde.Location = new Point(60, 48);
        dtpDesde.Size = new Size(130, 23);
        dtpDesde.Format = DateTimePickerFormat.Short;

        lblHasta.Text = "Hasta:";
        lblHasta.Location = new Point(202, 52);
        lblHasta.AutoSize = true;

        dtpHasta.Location = new Point(248, 48);
        dtpHasta.Size = new Size(130, 23);
        dtpHasta.Format = DateTimePickerFormat.Short;

        lblProveedorFiltro.Text = "Proveedor:";
        lblProveedorFiltro.Location = new Point(392, 52);
        lblProveedorFiltro.AutoSize = true;

        cboProveedor.Location = new Point(460, 48);
        cboProveedor.Size = new Size(160, 23);
        cboProveedor.DropDownStyle = ComboBoxStyle.DropDownList;

        btnBuscar.Location = new Point(632, 46);
        btnBuscar.Size = new Size(80, 27);
        btnBuscar.Text = "Buscar";
        btnBuscar.Click += btnBuscar_Click;

        dgvCompras.Location = new Point(12, 85);
        dgvCompras.Size = new Size(720, 200);
        dgvCompras.ReadOnly = true;
        dgvCompras.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCompras.MultiSelect = false;
        dgvCompras.AllowUserToAddRows = false;
        dgvCompras.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCompras.SelectionChanged += dgvCompras_SelectionChanged;

        lblTotalCompras.Location = new Point(12, 292);
        lblTotalCompras.Size = new Size(720, 18);
        lblTotalCompras.AutoSize = false;
        lblTotalCompras.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblTotalCompras.ForeColor = Color.DarkSlateBlue;

        lblDetalleTitle.Text = "Detalle de la compra seleccionada:";
        lblDetalleTitle.Location = new Point(12, 314);
        lblDetalleTitle.AutoSize = true;
        lblDetalleTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        dgvDetalle.Location = new Point(12, 334);
        dgvDetalle.Size = new Size(720, 180);
        dgvDetalle.ReadOnly = true;
        dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDetalle.AllowUserToAddRows = false;
        dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;



        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(750, 535);
        Text = "Lácteos Madeline – Consulta de Compras";
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            lblTitulo,
            lblDesde, dtpDesde, lblHasta, dtpHasta,
            lblProveedorFiltro, cboProveedor, btnBuscar,
            dgvCompras, lblTotalCompras, lblDetalleTitle, dgvDetalle
        });

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
