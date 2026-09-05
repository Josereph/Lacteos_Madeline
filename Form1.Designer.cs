namespace LacteosMadeline;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    // Fase 1
    private Label lblTitulo;
    private Button btnCategorias;
    private Button btnProductos;
    private Button btnVentas;

    // Fase 2
    private Button btnProveedores;
    private Button btnCompras;
    private Button btnCaja;
    private Button btnConsultaVentas;
    private Button btnConsultaCompras;
    private Button btnReportes;
    private Button btnBackup;
    private Button btnSalir;

    private Label lblSeparador1;
    private Label lblSeparador2;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo         = new Label();
        lblSeparador1     = new Label();
        lblSeparador2     = new Label();
        btnCategorias     = new Button();
        btnProductos      = new Button();
        btnVentas         = new Button();
        btnProveedores    = new Button();
        btnCompras        = new Button();
        btnCaja           = new Button();
        btnConsultaVentas = new Button();
        btnConsultaCompras = new Button();
        btnReportes       = new Button();
        btnBackup         = new Button();
        btnSalir          = new Button();

        SuspendLayout();

        // ── Título ─────────────────────────────────────────────────────
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitulo.Location = new Point(30, 20);
        lblTitulo.Text = "Lácteos Madeline";

        // ── Separadores de sección ─────────────────────────────────────
        lblSeparador1.AutoSize = true;
        lblSeparador1.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        lblSeparador1.ForeColor = Color.Gray;
        lblSeparador1.Location = new Point(30, 73);
        lblSeparador1.Text = "─── CATÁLOGO Y VENTAS ────────────────";

        lblSeparador2.AutoSize = true;
        lblSeparador2.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        lblSeparador2.ForeColor = Color.Gray;
        lblSeparador2.Location = new Point(30, 228);
        lblSeparador2.Text = "─── ADMINISTRACIÓN ───────────────────";

        // ── Columna izquierda (x=30) ───────────────────────────────────
        int xl = 30, xr = 165, w = 125, h = 38, gap = 10;

        // Fase 1 — fila 1
        int y1 = 93;
        ConfigurarBoton(btnCategorias, "Categorías",   xl, y1, w, h);
        ConfigurarBoton(btnProductos,  "Productos",    xr, y1, w, h);

        // Fase 1 — fila 2
        int y2 = y1 + h + gap;
        btnVentas.Location = new Point(xl, y2);
        btnVentas.Size = new Size(w * 2 + 5, h);
        btnVentas.Text = "Nueva Venta";
        btnVentas.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnVentas.Click += btnVentas_Click;

        // ── Fase 2 ────────────────────────────────────────────────────
        int y3 = 248;
        ConfigurarBoton(btnProveedores,    "Proveedores",    xl, y3, w, h);
        ConfigurarBoton(btnCompras,        "Compras",        xr, y3, w, h);

        int y4 = y3 + h + gap;
        ConfigurarBoton(btnCaja,           "Control de Caja",  xl, y4, w, h);
        ConfigurarBoton(btnConsultaVentas, "Consulta Ventas",  xr, y4, w, h);

        int y5 = y4 + h + gap;
        ConfigurarBoton(btnConsultaCompras, "Consulta Compras", xl, y5, w, h);
        ConfigurarBoton(btnReportes,         "Reportes",         xr, y5, w, h);

        int y6 = y5 + h + gap;
        ConfigurarBoton(btnBackup, "Respaldo",  xl, y6, w, h);

        // ── Salir ──────────────────────────────────────────────────────
        int y7 = y6 + h + 15;
        btnSalir.Location = new Point(xl, y7);
        btnSalir.Size = new Size(w * 2 + 5, 35);
        btnSalir.Text = "Salir";
        btnSalir.Click += btnSalir_Click;

        // ── Eventos Fase 1 ─────────────────────────────────────────────
        btnCategorias.Click += btnCategorias_Click;
        btnProductos.Click  += btnProductos_Click;
        btnVentas.Click     += btnVentas_Click;

        // ── Eventos Fase 2 ─────────────────────────────────────────────
        btnProveedores.Click    += btnProveedores_Click;
        btnCompras.Click        += btnCompras_Click;
        btnCaja.Click           += btnCaja_Click;
        btnConsultaVentas.Click += btnConsultaVentas_Click;
        btnConsultaCompras.Click += btnConsultaCompras_Click;
        btnReportes.Click       += btnReportes_Click;
        btnBackup.Click         += btnBackup_Click;

        // ── Form ───────────────────────────────────────────────────────
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(310, y7 + 60);
        Controls.AddRange(new Control[]
        {
            lblTitulo, lblSeparador1, lblSeparador2,
            btnCategorias, btnProductos, btnVentas,
            btnProveedores, btnCompras, btnCaja,
            btnConsultaVentas, btnConsultaCompras, btnReportes,
            btnBackup, btnSalir
        });
        Text = "Lácteos Madeline – Menú Principal";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        ResumeLayout(false);
        PerformLayout();
    }

    private static void ConfigurarBoton(Button btn, string texto, int x, int y, int w, int h)
    {
        btn.Location = new Point(x, y);
        btn.Size = new Size(w, h);
        btn.Text = texto;
    }

    #endregion
}
