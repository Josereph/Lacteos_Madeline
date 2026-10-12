namespace LacteosMadeline.Forms;

partial class FormCaja
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private Label lblTitulo;
    private Label lblEstado;

    // Panel apertura
    private Panel panelApertura;
    private Label lblMontoInicialLabel;
    private TextBox txtMontoInicial;
    private Button btnAbrir;

    // Panel operaciones (TabControl)
    private Panel panelOperaciones;
    private TabControl tabCaja;
    private TabPage tabJornada;
    private TabPage tabHistorial;

    // Tab Jornada
    private GroupBox grpMovimiento;
    private RadioButton rbEntrada;
    private RadioButton rbSalida;
    private Label lblMontoLabel;
    private TextBox txtMonto;
    private Label lblDescripcionLabel;
    private TextBox txtDescripcion;
    private Button btnRegistrarMovimiento;
    private DataGridView dgvMovimientos;
    private GroupBox grpResumen;
    private Label lblMontoInicial;
    private Label lblVentas;
    private Label lblVentasOtros;
    private Label lblEntradas;
    private Label lblSalidas;
    private Label lblSaldoEsperado;
    private Label lblCapInicial;
    private Label lblCapVentas;
    private Label lblCapEntradas;
    private Label lblCapSalidas;
    private Label lblCapSaldo;
    private Label lblLineaSaldo;
    private Button btnCerrarCaja;

    // Tab Historial
    private DataGridView dgvHistorial;

    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo        = new Label();
        lblEstado        = new Label();
        panelApertura    = new Panel();
        panelOperaciones = new Panel();
        tabCaja          = new TabControl();
        tabJornada       = new TabPage();
        tabHistorial     = new TabPage();
        grpMovimiento    = new GroupBox();
        grpResumen       = new GroupBox();
        dgvMovimientos   = new DataGridView();
        dgvHistorial     = new DataGridView();
        lblMensaje       = new Label();

        lblMontoInicialLabel   = new Label();
        txtMontoInicial        = new TextBox();
        btnAbrir               = new Button();
        rbEntrada              = new RadioButton();
        rbSalida               = new RadioButton();
        lblMontoLabel          = new Label();
        txtMonto               = new TextBox();
        lblDescripcionLabel    = new Label();
        txtDescripcion         = new TextBox();
        btnRegistrarMovimiento = new Button();
        lblMontoInicial        = new Label();
        lblVentas              = new Label();
        lblVentasOtros         = new Label();
        lblEntradas            = new Label();
        lblSalidas             = new Label();
        lblSaldoEsperado       = new Label();
        lblCapInicial          = new Label();
        lblCapVentas           = new Label();
        lblCapEntradas         = new Label();
        lblCapSalidas          = new Label();
        lblCapSaldo            = new Label();
        lblLineaSaldo          = new Label();
        btnCerrarCaja          = new Button();

        SuspendLayout();

        // ─── Encabezado ────────────────────────────────────────────────────
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 12);
        lblTitulo.Text = "Control de Caja";

        lblEstado.Location = new Point(12, 46);
        lblEstado.Size = new Size(760, 20);
        lblEstado.AutoSize = false;
        lblEstado.Font = new Font("Segoe UI", 9F, FontStyle.Italic);

        // ─── Panel Apertura ────────────────────────────────────────────────
        panelApertura.Location = new Point(12, 75);
        panelApertura.Size = new Size(430, 50);
        panelApertura.BorderStyle = BorderStyle.FixedSingle;

        lblMontoInicialLabel.Text = "Monto inicial ($):";
        lblMontoInicialLabel.Location = new Point(8, 14);
        lblMontoInicialLabel.AutoSize = true;

        txtMontoInicial.Location = new Point(130, 11);
        txtMontoInicial.Size = new Size(110, 23);
        txtMontoInicial.PlaceholderText = "0.00";
        txtMontoInicial.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnAbrir_Click(s, e); };

        btnAbrir.Location = new Point(250, 10);
        btnAbrir.Size = new Size(110, 28);
        btnAbrir.Text = "Abrir Caja";
        btnAbrir.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnAbrir.BackColor = Color.SeaGreen;
        btnAbrir.ForeColor = Color.White;
        btnAbrir.Click += btnAbrir_Click;

        panelApertura.Controls.AddRange(new Control[] { lblMontoInicialLabel, txtMontoInicial, btnAbrir });

        // ─── Panel Operaciones ─────────────────────────────────────────────
        panelOperaciones.Location = new Point(12, 75);
        panelOperaciones.Size = new Size(780, 495);
        panelOperaciones.Visible = false;

        // TabControl dentro del panel operaciones
        tabCaja.Location = new Point(0, 0);
        tabCaja.Size = new Size(780, 490);
        tabCaja.Parent = panelOperaciones;

        tabJornada.Text = "Jornada actual";
        tabHistorial.Text = "Historial de cajas";

        tabCaja.TabPages.AddRange(new TabPage[] { tabJornada, tabHistorial });

        // ── Tab Jornada ──────────────────────────────────────────────────

        // GroupBox movimiento
        grpMovimiento.Text = "Registrar movimiento";
        grpMovimiento.Location = new Point(5, 8);
        grpMovimiento.Size = new Size(760, 80);
        grpMovimiento.Parent = tabJornada;

        rbEntrada.Text = "Entrada";
        rbEntrada.Location = new Point(10, 28);
        rbEntrada.AutoSize = true;
        rbEntrada.Checked = true;
        rbEntrada.Parent = grpMovimiento;

        rbSalida.Text = "Salida";
        rbSalida.Location = new Point(90, 28);
        rbSalida.AutoSize = true;
        rbSalida.Parent = grpMovimiento;

        lblMontoLabel.Text = "Monto ($):";
        lblMontoLabel.Location = new Point(180, 30);
        lblMontoLabel.AutoSize = true;
        lblMontoLabel.Parent = grpMovimiento;

        txtMonto.Location = new Point(248, 27);
        txtMonto.Size = new Size(90, 23);
        txtMonto.PlaceholderText = "0.00";
        txtMonto.Parent = grpMovimiento;

        lblDescripcionLabel.Text = "Descripción:";
        lblDescripcionLabel.Location = new Point(350, 30);
        lblDescripcionLabel.AutoSize = true;
        lblDescripcionLabel.Parent = grpMovimiento;

        txtDescripcion.Location = new Point(435, 27);
        txtDescripcion.Size = new Size(210, 23);
        txtDescripcion.Parent = grpMovimiento;

        btnRegistrarMovimiento.Location = new Point(655, 25);
        btnRegistrarMovimiento.Size = new Size(95, 28);
        btnRegistrarMovimiento.Text = "Registrar";
        btnRegistrarMovimiento.Click += btnRegistrarMovimiento_Click;
        btnRegistrarMovimiento.Parent = grpMovimiento;

        grpMovimiento.Controls.AddRange(new Control[]
        {
            rbEntrada, rbSalida, lblMontoLabel, txtMonto,
            lblDescripcionLabel, txtDescripcion, btnRegistrarMovimiento
        });

        // Grid de movimientos
        dgvMovimientos.Location = new Point(5, 97);
        dgvMovimientos.Size = new Size(760, 185);
        dgvMovimientos.ReadOnly = true;
        dgvMovimientos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvMovimientos.AllowUserToAddRows = false;
        dgvMovimientos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvMovimientos.Parent = tabJornada;

        // GroupBox resumen
        grpResumen.Text = "Resumen de la jornada";
        grpResumen.Location = new Point(5, 290);
        grpResumen.Size = new Size(760, 160);
        grpResumen.Parent = tabJornada;

        // Columna de conceptos (izquierda) y columna de montos alineados a la
        // derecha: legible de un vistazo sin depender de una fuente monoespaciada.
        Font fConcepto = new Font("Segoe UI", 9.75F);
        Font fMonto = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        int cx = 14, vx = 190, ry = 26, fila = 22;

        Label Concepto(Label l, string texto, int y)
        {
            l.Text = texto; l.Location = new Point(cx, y); l.Size = new Size(170, 20);
            l.Font = fConcepto; l.Parent = grpResumen;
            return l;
        }

        void Monto(Label l, int y)
        {
            l.Location = new Point(vx, y); l.Size = new Size(130, 20);
            l.Font = fMonto; l.TextAlign = ContentAlignment.MiddleRight; l.Parent = grpResumen;
        }

        Concepto(lblCapInicial,  "Fondo inicial",        ry);            Monto(lblMontoInicial, ry);
        Concepto(lblCapVentas,   "+ Ventas en efectivo",  ry + fila);     Monto(lblVentas,       ry + fila);
        Concepto(lblCapEntradas, "+ Entradas manuales",  ry + fila * 2); Monto(lblEntradas,     ry + fila * 2);
        Concepto(lblCapSalidas,  "− Salidas manuales",   ry + fila * 3); Monto(lblSalidas,      ry + fila * 3);

        lblLineaSaldo.Location = new Point(cx, ry + fila * 4 + 2);
        lblLineaSaldo.Size = new Size(vx + 130 - cx, 1);
        lblLineaSaldo.BorderStyle = BorderStyle.FixedSingle;
        lblLineaSaldo.Parent = grpResumen;

        Concepto(lblCapSaldo, "Efectivo esperado", ry + fila * 4 + 8).Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblCapSaldo.Size = new Size(170, 24);
        Monto(lblSaldoEsperado, ry + fila * 4 + 8);
        lblSaldoEsperado.Size = new Size(130, 24);
        lblSaldoEsperado.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

        lblVentasOtros.Location = new Point(350, ry); lblVentasOtros.Size = new Size(230, 72);
        lblVentasOtros.Font = new Font("Segoe UI", 9F, FontStyle.Italic); lblVentasOtros.ForeColor = Color.DimGray;
        lblVentasOtros.Parent = grpResumen;

        btnCerrarCaja.Location = new Point(600, 44);
        btnCerrarCaja.Size = new Size(146, 56);
        btnCerrarCaja.Text = "Cerrar Caja";
        btnCerrarCaja.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnCerrarCaja.BackColor = Color.Firebrick;
        btnCerrarCaja.ForeColor = Color.White;
        btnCerrarCaja.Click += btnCerrarCaja_Click;
        btnCerrarCaja.Parent = grpResumen;

        grpResumen.Controls.AddRange(new Control[]
        {
            lblCapInicial, lblCapVentas, lblCapEntradas, lblCapSalidas, lblCapSaldo, lblLineaSaldo,
            lblMontoInicial, lblVentas, lblVentasOtros, lblEntradas, lblSalidas, lblSaldoEsperado, btnCerrarCaja
        });

        tabJornada.Controls.AddRange(new Control[] { grpMovimiento, dgvMovimientos, grpResumen });

        // ── Tab Historial ────────────────────────────────────────────────
        dgvHistorial.Location = new Point(5, 8);
        dgvHistorial.Size = new Size(760, 445);
        dgvHistorial.ReadOnly = true;
        dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvHistorial.AllowUserToAddRows = false;
        dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvHistorial.Parent = tabHistorial;

        tabHistorial.Controls.Add(dgvHistorial);

        panelOperaciones.Controls.Add(tabCaja);

        // lblMensaje
        lblMensaje.Location = new Point(12, 578);
        lblMensaje.Size = new Size(780, 20);
        lblMensaje.AutoSize = false;

        // Form
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(804, 608);
        Text = "Lácteos Madeline – Control de Caja";
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblEstado,
            panelApertura, panelOperaciones,
            lblMensaje
        });

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
