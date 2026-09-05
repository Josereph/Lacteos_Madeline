namespace LacteosMadeline.Forms;

partial class FormCompras
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private Label lblTitulo;
    private Label lblProveedor;
    private ComboBox cboProveedor;
    private Label lblProducto;
    private ComboBox cboProducto;
    private Label lblCantidad;
    private NumericUpDown nudCantidad;
    private Label lblCosto;
    private TextBox txtCosto;
    private Button btnAgregar;
    private DataGridView dgvDetalle;
    private Button btnEliminarLinea;
    private Label lblTotal;
    private Button btnConfirmar;
    private Button btnCancelar;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo       = new Label();
        lblProveedor    = new Label();
        cboProveedor    = new ComboBox();
        lblProducto     = new Label();
        cboProducto     = new ComboBox();
        lblCantidad     = new Label();
        nudCantidad     = new NumericUpDown();
        lblCosto        = new Label();
        txtCosto        = new TextBox();
        btnAgregar      = new Button();
        dgvDetalle      = new DataGridView();
        btnEliminarLinea = new Button();
        lblTotal        = new Label();
        btnConfirmar    = new Button();
        btnCancelar     = new Button();
        lblMensaje      = new Label();

        SuspendLayout();

        // lblTitulo
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 12);
        lblTitulo.Text = "Nueva Compra";

        // Fila proveedor
        lblProveedor.Text = "Proveedor:";
        lblProveedor.Location = new Point(12, 50);
        lblProveedor.AutoSize = true;

        cboProveedor.Location = new Point(90, 47);
        cboProveedor.Size = new Size(300, 23);
        cboProveedor.DropDownStyle = ComboBoxStyle.DropDownList;

        // Fila selección producto
        lblProducto.Text = "Producto:";
        lblProducto.Location = new Point(12, 85);
        lblProducto.AutoSize = true;

        cboProducto.Location = new Point(90, 82);
        cboProducto.Size = new Size(240, 23);
        cboProducto.DropDownStyle = ComboBoxStyle.DropDownList;

        lblCantidad.Text = "Cantidad:";
        lblCantidad.Location = new Point(340, 85);
        lblCantidad.AutoSize = true;

        nudCantidad.Location = new Point(410, 82);
        nudCantidad.Size = new Size(60, 23);
        nudCantidad.Minimum = 1;
        nudCantidad.Maximum = 99999;
        nudCantidad.Value = 1;

        lblCosto.Text = "Costo:";
        lblCosto.Location = new Point(480, 85);
        lblCosto.AutoSize = true;

        txtCosto.Location = new Point(525, 82);
        txtCosto.Size = new Size(90, 23);
        txtCosto.PlaceholderText = "0.00";

        btnAgregar.Location = new Point(625, 80);
        btnAgregar.Size = new Size(90, 27);
        btnAgregar.Text = "Agregar";
        btnAgregar.Click += btnAgregar_Click;

        // dgvDetalle
        dgvDetalle.Location = new Point(12, 120);
        dgvDetalle.Size = new Size(720, 220);
        dgvDetalle.ReadOnly = true;
        dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDetalle.MultiSelect = false;
        dgvDetalle.AllowUserToAddRows = false;
        dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // btnEliminarLinea
        btnEliminarLinea.Location = new Point(12, 350);
        btnEliminarLinea.Size = new Size(140, 28);
        btnEliminarLinea.Text = "Eliminar línea";
        btnEliminarLinea.Click += btnEliminarLinea_Click;

        // lblTotal
        lblTotal.Location = new Point(400, 354);
        lblTotal.Size = new Size(200, 22);
        lblTotal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblTotal.Text = "Total: $0.00";
        lblTotal.TextAlign = ContentAlignment.MiddleRight;

        // Botones confirmar / cancelar
        btnConfirmar.Location = new Point(555, 350);
        btnConfirmar.Size = new Size(80, 28);
        btnConfirmar.Text = "Confirmar";
        btnConfirmar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnConfirmar.Click += btnConfirmar_Click;

        btnCancelar.Location = new Point(645, 350);
        btnCancelar.Size = new Size(80, 28);
        btnCancelar.Text = "Cancelar";
        btnCancelar.Click += btnCancelar_Click;

        // lblMensaje
        lblMensaje.Location = new Point(12, 390);
        lblMensaje.Size = new Size(720, 20);
        lblMensaje.AutoSize = false;

        // Form
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(750, 420);
        Text = "Lácteos Madeline – Nueva Compra";
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            lblTitulo,
            lblProveedor, cboProveedor,
            lblProducto, cboProducto,
            lblCantidad, nudCantidad,
            lblCosto, txtCosto,
            btnAgregar,
            dgvDetalle,
            btnEliminarLinea,
            lblTotal,
            btnConfirmar, btnCancelar,
            lblMensaje
        });

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
