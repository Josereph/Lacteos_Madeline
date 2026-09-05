namespace LacteosMadeline.Forms;

partial class FormVentas
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

    private Label lblProducto;
    private ComboBox cboProducto;
    private Label lblCantidad;
    private NumericUpDown nudCantidad;
    private Button btnAgregar;

    private DataGridView dgvCarrito;
    private Button btnEliminarLinea;

    private Label lblTotal;
    private Button btnConfirmar;
    private Button btnCancelar;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblProducto = new Label();
        cboProducto = new ComboBox();
        lblCantidad = new Label();
        nudCantidad = new NumericUpDown();
        btnAgregar = new Button();

        dgvCarrito = new DataGridView();
        btnEliminarLinea = new Button();

        lblTotal = new Label();
        btnConfirmar = new Button();
        btnCancelar = new Button();
        lblMensaje = new Label();

        ((System.ComponentModel.ISupportInitialize)nudCantidad).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvCarrito).BeginInit();
        SuspendLayout();

        // lblProducto
        lblProducto.AutoSize = true;
        lblProducto.Location = new Point(15, 18);
        lblProducto.Text = "Producto:";

        // cboProducto
        cboProducto.Location = new Point(90, 15);
        cboProducto.Size = new Size(300, 23);
        cboProducto.DropDownStyle = ComboBoxStyle.DropDownList;

        // lblCantidad
        lblCantidad.AutoSize = true;
        lblCantidad.Location = new Point(400, 18);
        lblCantidad.Text = "Cantidad:";

        // nudCantidad
        nudCantidad.Location = new Point(465, 15);
        nudCantidad.Size = new Size(70, 23);
        nudCantidad.Minimum = 1;
        nudCantidad.Maximum = 100000;
        nudCantidad.Value = 1;

        // btnAgregar
        btnAgregar.Location = new Point(545, 14);
        btnAgregar.Size = new Size(90, 25);
        btnAgregar.Text = "Agregar";
        btnAgregar.Click += btnAgregar_Click;

        // dgvCarrito
        dgvCarrito.Location = new Point(15, 50);
        dgvCarrito.Size = new Size(620, 220);
        dgvCarrito.ReadOnly = true;
        dgvCarrito.AllowUserToAddRows = false;
        dgvCarrito.AllowUserToDeleteRows = false;
        dgvCarrito.MultiSelect = false;
        dgvCarrito.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCarrito.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // btnEliminarLinea
        btnEliminarLinea.Location = new Point(15, 280);
        btnEliminarLinea.Size = new Size(220, 28);
        btnEliminarLinea.Text = "Eliminar producto seleccionado";
        btnEliminarLinea.Click += btnEliminarLinea_Click;

        // lblTotal
        lblTotal.AutoSize = true;
        lblTotal.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTotal.Location = new Point(430, 278);
        lblTotal.Text = "Total: $0.00";

        // btnConfirmar
        btnConfirmar.Location = new Point(15, 320);
        btnConfirmar.Size = new Size(160, 35);
        btnConfirmar.Text = "Confirmar Venta";
        btnConfirmar.Click += btnConfirmar_Click;

        // btnCancelar
        btnCancelar.Location = new Point(185, 320);
        btnCancelar.Size = new Size(160, 35);
        btnCancelar.Text = "Cancelar Venta";
        btnCancelar.Click += btnCancelar_Click;

        // lblMensaje
        lblMensaje.AutoSize = true;
        lblMensaje.ForeColor = Color.DarkRed;
        lblMensaje.Location = new Point(15, 365);
        lblMensaje.Size = new Size(620, 20);

        // FormVentas
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(650, 400);
        Controls.Add(lblProducto);
        Controls.Add(cboProducto);
        Controls.Add(lblCantidad);
        Controls.Add(nudCantidad);
        Controls.Add(btnAgregar);
        Controls.Add(dgvCarrito);
        Controls.Add(btnEliminarLinea);
        Controls.Add(lblTotal);
        Controls.Add(btnConfirmar);
        Controls.Add(btnCancelar);
        Controls.Add(lblMensaje);
        Text = "Nueva Venta";
        StartPosition = FormStartPosition.CenterParent;
        ((System.ComponentModel.ISupportInitialize)nudCantidad).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvCarrito).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
