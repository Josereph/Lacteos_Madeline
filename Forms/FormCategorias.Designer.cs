namespace LacteosMadeline.Forms;

partial class FormCategorias
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

    private DataGridView dgvCategorias;
    private Label lblNombre;
    private TextBox txtNombre;
    private Button btnNuevo;
    private Button btnGuardar;
    private Button btnActivarDesactivar;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        dgvCategorias = new DataGridView();
        lblNombre = new Label();
        txtNombre = new TextBox();
        btnNuevo = new Button();
        btnGuardar = new Button();
        btnActivarDesactivar = new Button();
        lblMensaje = new Label();
        ((System.ComponentModel.ISupportInitialize)dgvCategorias).BeginInit();
        SuspendLayout();

        // dgvCategorias
        dgvCategorias.Location = new Point(15, 15);
        dgvCategorias.Size = new Size(420, 260);
        dgvCategorias.ReadOnly = true;
        dgvCategorias.AllowUserToAddRows = false;
        dgvCategorias.AllowUserToDeleteRows = false;
        dgvCategorias.MultiSelect = false;
        dgvCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCategorias.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCategorias.SelectionChanged += dgvCategorias_SelectionChanged;

        // lblNombre
        lblNombre.AutoSize = true;
        lblNombre.Location = new Point(15, 290);
        lblNombre.Text = "Nombre de la categoría:";

        // txtNombre
        txtNombre.Location = new Point(15, 312);
        txtNombre.Size = new Size(280, 23);

        // btnNuevo
        btnNuevo.Location = new Point(305, 310);
        btnNuevo.Size = new Size(90, 27);
        btnNuevo.Text = "Nuevo";
        btnNuevo.Click += btnNuevo_Click;

        // btnGuardar
        btnGuardar.Location = new Point(15, 345);
        btnGuardar.Size = new Size(120, 30);
        btnGuardar.Text = "Guardar";
        btnGuardar.Click += btnGuardar_Click;

        // btnActivarDesactivar
        btnActivarDesactivar.Location = new Point(145, 345);
        btnActivarDesactivar.Size = new Size(170, 30);
        btnActivarDesactivar.Text = "Activar / Desactivar";
        btnActivarDesactivar.Click += btnActivarDesactivar_Click;

        // lblMensaje
        lblMensaje.AutoSize = true;
        lblMensaje.ForeColor = Color.DarkRed;
        lblMensaje.Location = new Point(15, 385);
        lblMensaje.Size = new Size(420, 20);

        // FormCategorias
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(450, 420);
        Controls.Add(dgvCategorias);
        Controls.Add(lblNombre);
        Controls.Add(txtNombre);
        Controls.Add(btnNuevo);
        Controls.Add(btnGuardar);
        Controls.Add(btnActivarDesactivar);
        Controls.Add(lblMensaje);
        Text = "Categorías";
        StartPosition = FormStartPosition.CenterParent;
        ((System.ComponentModel.ISupportInitialize)dgvCategorias).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
