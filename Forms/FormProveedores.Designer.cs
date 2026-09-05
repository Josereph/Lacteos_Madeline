namespace LacteosMadeline.Forms;

partial class FormProveedores
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private Label lblTitulo;
    private TextBox txtBuscar;
    private Button btnBuscar;
    private DataGridView dgvProveedores;

    private Label lblNombre;
    private TextBox txtNombre;
    private Label lblContacto;
    private TextBox txtContacto;
    private Label lblTelefono;
    private TextBox txtTelefono;
    private Label lblDireccion;
    private TextBox txtDireccion;
    private Label lblObservaciones;
    private TextBox txtObservaciones;

    private Button btnNuevo;
    private Button btnGuardar;
    private Button btnActivarDesactivar;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo           = new Label();
        txtBuscar           = new TextBox();
        btnBuscar           = new Button();
        dgvProveedores      = new DataGridView();
        lblNombre           = new Label();
        txtNombre           = new TextBox();
        lblContacto         = new Label();
        txtContacto         = new TextBox();
        lblTelefono         = new Label();
        txtTelefono         = new TextBox();
        lblDireccion        = new Label();
        txtDireccion        = new TextBox();
        lblObservaciones    = new Label();
        txtObservaciones    = new TextBox();
        btnNuevo            = new Button();
        btnGuardar          = new Button();
        btnActivarDesactivar = new Button();
        lblMensaje          = new Label();

        SuspendLayout();

        // lblTitulo
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 12);
        lblTitulo.Text = "Proveedores";

        // txtBuscar
        txtBuscar.Location = new Point(12, 50);
        txtBuscar.Size = new Size(280, 23);
        txtBuscar.PlaceholderText = "Buscar por nombre...";
        txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnBuscar_Click(s, e); };

        // btnBuscar
        btnBuscar.Location = new Point(302, 49);
        btnBuscar.Size = new Size(80, 25);
        btnBuscar.Text = "Buscar";
        btnBuscar.Click += btnBuscar_Click;

        // dgvProveedores
        dgvProveedores.Location = new Point(12, 85);
        dgvProveedores.Size = new Size(560, 220);
        dgvProveedores.ReadOnly = true;
        dgvProveedores.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvProveedores.MultiSelect = false;
        dgvProveedores.AllowUserToAddRows = false;
        dgvProveedores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvProveedores.SelectionChanged += dgvProveedores_SelectionChanged;

        // Panel de edición — etiquetas y campos
        int px = 590, py = 85, sepY = 30;

        lblNombre.Text = "Nombre *";
        lblNombre.Location = new Point(px, py);
        txtNombre.Location = new Point(px, py + 18);
        txtNombre.Size = new Size(240, 23);

        lblContacto.Text = "Contacto";
        lblContacto.Location = new Point(px, py += sepY + 18);
        txtContacto.Location = new Point(px, py + 18);
        txtContacto.Size = new Size(240, 23);

        lblTelefono.Text = "Teléfono";
        lblTelefono.Location = new Point(px, py += sepY + 18);
        txtTelefono.Location = new Point(px, py + 18);
        txtTelefono.Size = new Size(240, 23);

        lblDireccion.Text = "Dirección";
        lblDireccion.Location = new Point(px, py += sepY + 18);
        txtDireccion.Location = new Point(px, py + 18);
        txtDireccion.Size = new Size(240, 23);

        lblObservaciones.Text = "Observaciones";
        lblObservaciones.Location = new Point(px, py += sepY + 18);
        txtObservaciones.Location = new Point(px, py + 18);
        txtObservaciones.Size = new Size(240, 50);
        txtObservaciones.Multiline = true;

        py += 70;

        // btnNuevo
        btnNuevo.Location = new Point(px, py + 10);
        btnNuevo.Size = new Size(110, 30);
        btnNuevo.Text = "Nuevo";
        btnNuevo.Click += btnNuevo_Click;

        // btnGuardar
        btnGuardar.Location = new Point(px + 125, py + 10);
        btnGuardar.Size = new Size(110, 30);
        btnGuardar.Text = "Guardar";
        btnGuardar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnGuardar.Click += btnGuardar_Click;

        // btnActivarDesactivar
        btnActivarDesactivar.Location = new Point(px, py + 50);
        btnActivarDesactivar.Size = new Size(240, 30);
        btnActivarDesactivar.Text = "Activar / Desactivar";
        btnActivarDesactivar.Click += btnActivarDesactivar_Click;

        // lblMensaje
        lblMensaje.Location = new Point(12, 315);
        lblMensaje.Size = new Size(560, 20);
        lblMensaje.AutoSize = false;

        // Form
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(850, 350);
        Text = "Lácteos Madeline – Proveedores";
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            lblTitulo, txtBuscar, btnBuscar, dgvProveedores,
            lblNombre, txtNombre,
            lblContacto, txtContacto,
            lblTelefono, txtTelefono,
            lblDireccion, txtDireccion,
            lblObservaciones, txtObservaciones,
            btnNuevo, btnGuardar, btnActivarDesactivar,
            lblMensaje
        });

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
