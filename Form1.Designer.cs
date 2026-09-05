namespace LacteosMadeline;

partial class Form1
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

    #region Windows Form Designer generated code

    private Label lblTitulo;
    private Button btnCategorias;
    private Button btnProductos;
    private Button btnVentas;
    private Button btnSalir;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblTitulo = new Label();
        btnCategorias = new Button();
        btnProductos = new Button();
        btnVentas = new Button();
        btnSalir = new Button();
        SuspendLayout();

        // lblTitulo
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitulo.Location = new Point(40, 30);
        lblTitulo.Text = "Lácteos Madeline";

        // btnCategorias
        btnCategorias.Location = new Point(40, 110);
        btnCategorias.Size = new Size(220, 45);
        btnCategorias.Text = "Categorías";
        btnCategorias.Click += btnCategorias_Click;

        // btnProductos
        btnProductos.Location = new Point(40, 165);
        btnProductos.Size = new Size(220, 45);
        btnProductos.Text = "Productos";
        btnProductos.Click += btnProductos_Click;

        // btnVentas
        btnVentas.Location = new Point(40, 220);
        btnVentas.Size = new Size(220, 45);
        btnVentas.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnVentas.Text = "Nueva Venta";
        btnVentas.Click += btnVentas_Click;

        // btnSalir
        btnSalir.Location = new Point(40, 285);
        btnSalir.Size = new Size(220, 40);
        btnSalir.Text = "Salir";
        btnSalir.Click += btnSalir_Click;

        // Form1
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(320, 360);
        Controls.Add(lblTitulo);
        Controls.Add(btnCategorias);
        Controls.Add(btnProductos);
        Controls.Add(btnVentas);
        Controls.Add(btnSalir);
        Text = "Lácteos Madeline - Menú Principal";
        StartPosition = FormStartPosition.CenterScreen;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
