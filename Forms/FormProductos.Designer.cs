namespace LacteosMadeline.Forms;

partial class FormProductos
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private TextBox txtBuscar;
    private Button btnBuscar;
    private DataGridView dgvProductos;

    private Label lblNombre;
    private TextBox txtNombre;
    private Label lblCategoria;
    private ComboBox cboCategoria;
    private Label lblPrecioCompra;
    private TextBox txtPrecioCompra;
    private Label lblPrecioVenta;
    private TextBox txtPrecioVenta;
    private Label lblExistencia;
    private TextBox txtExistencia;
    private Label lblStockMinimo;
    private TextBox txtStockMinimo;

    private Button btnNuevo;
    private Button btnGuardar;
    private Button btnActivarDesactivar;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        txtBuscar = new TextBox();
        btnBuscar = new Button();
        dgvProductos = new DataGridView();

        lblNombre = new Label();
        txtNombre = new TextBox();
        lblCategoria = new Label();
        cboCategoria = new ComboBox();
        lblPrecioCompra = new Label();
        txtPrecioCompra = new TextBox();
        lblPrecioVenta = new Label();
        txtPrecioVenta = new TextBox();
        lblExistencia = new Label();
        txtExistencia = new TextBox();
        lblStockMinimo = new Label();
        txtStockMinimo = new TextBox();

        btnNuevo = new Button();
        btnGuardar = new Button();
        btnActivarDesactivar = new Button();
        lblMensaje = new Label();

        ((System.ComponentModel.ISupportInitialize)dgvProductos).BeginInit();
        SuspendLayout();

        // txtBuscar
        txtBuscar.Location = new Point(15, 15);
        txtBuscar.Size = new Size(300, 23);
        txtBuscar.PlaceholderText = "Buscar producto por nombre...";

        // btnBuscar
        btnBuscar.Location = new Point(325, 14);
        btnBuscar.Size = new Size(90, 25);
        btnBuscar.Text = "Buscar";
        btnBuscar.Click += btnBuscar_Click;

        // dgvProductos
        dgvProductos.Location = new Point(15, 50);
        dgvProductos.Size = new Size(600, 220);
        dgvProductos.ReadOnly = true;
        dgvProductos.AllowUserToAddRows = false;
        dgvProductos.AllowUserToDeleteRows = false;
        dgvProductos.MultiSelect = false;
        dgvProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvProductos.SelectionChanged += dgvProductos_SelectionChanged;

        // lblNombre
        lblNombre.AutoSize = true;
        lblNombre.Location = new Point(15, 285);
        lblNombre.Text = "Nombre:";

        // txtNombre
        txtNombre.Location = new Point(120, 282);
        txtNombre.Size = new Size(200, 23);

        // lblCategoria
        lblCategoria.AutoSize = true;
        lblCategoria.Location = new Point(340, 285);
        lblCategoria.Text = "Categoría:";

        // cboCategoria
        cboCategoria.Location = new Point(410, 282);
        cboCategoria.Size = new Size(200, 23);
        cboCategoria.DropDownStyle = ComboBoxStyle.DropDownList;

        // lblPrecioCompra
        lblPrecioCompra.AutoSize = true;
        lblPrecioCompra.Location = new Point(15, 320);
        lblPrecioCompra.Text = "Precio compra:";

        // txtPrecioCompra
        txtPrecioCompra.Location = new Point(120, 317);
        txtPrecioCompra.Size = new Size(100, 23);

        // lblPrecioVenta
        lblPrecioVenta.AutoSize = true;
        lblPrecioVenta.Location = new Point(240, 320);
        lblPrecioVenta.Text = "Precio venta:";

        // txtPrecioVenta
        txtPrecioVenta.Location = new Point(340, 317);
        txtPrecioVenta.Size = new Size(100, 23);

        // lblExistencia
        lblExistencia.AutoSize = true;
        lblExistencia.Location = new Point(460, 320);
        lblExistencia.Text = "Existencia:";

        // txtExistencia
        txtExistencia.Location = new Point(535, 317);
        txtExistencia.Size = new Size(75, 23);

        // lblStockMinimo
        lblStockMinimo.AutoSize = true;
        lblStockMinimo.Location = new Point(15, 355);
        lblStockMinimo.Text = "Stock mínimo:";

        // txtStockMinimo
        txtStockMinimo.Location = new Point(120, 352);
        txtStockMinimo.Size = new Size(100, 23);

        // btnNuevo
        btnNuevo.Location = new Point(15, 390);
        btnNuevo.Size = new Size(90, 30);
        btnNuevo.Text = "Nuevo";
        btnNuevo.Click += btnNuevo_Click;

        // btnGuardar
        btnGuardar.Location = new Point(115, 390);
        btnGuardar.Size = new Size(120, 30);
        btnGuardar.Text = "Guardar";
        btnGuardar.Click += btnGuardar_Click;

        // btnActivarDesactivar
        btnActivarDesactivar.Location = new Point(245, 390);
        btnActivarDesactivar.Size = new Size(170, 30);
        btnActivarDesactivar.Text = "Activar / Desactivar";
        btnActivarDesactivar.Click += btnActivarDesactivar_Click;

        // lblMensaje
        lblMensaje.AutoSize = true;
        lblMensaje.ForeColor = Color.DarkRed;
        lblMensaje.Location = new Point(15, 430);
        lblMensaje.Size = new Size(600, 20);

        // FormProductos
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(630, 465);
        Controls.Add(txtBuscar);
        Controls.Add(btnBuscar);
        Controls.Add(dgvProductos);
        Controls.Add(lblNombre);
        Controls.Add(txtNombre);
        Controls.Add(lblCategoria);
        Controls.Add(cboCategoria);
        Controls.Add(lblPrecioCompra);
        Controls.Add(txtPrecioCompra);
        Controls.Add(lblPrecioVenta);
        Controls.Add(txtPrecioVenta);
        Controls.Add(lblExistencia);
        Controls.Add(txtExistencia);
        Controls.Add(lblStockMinimo);
        Controls.Add(txtStockMinimo);
        Controls.Add(btnNuevo);
        Controls.Add(btnGuardar);
        Controls.Add(btnActivarDesactivar);
        Controls.Add(lblMensaje);
        Text = "Productos";
        StartPosition = FormStartPosition.CenterParent;
        ((System.ComponentModel.ISupportInitialize)dgvProductos).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
