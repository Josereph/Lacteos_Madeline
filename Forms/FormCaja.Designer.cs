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
    private Label lblEntradas;
    private Label lblSalidas;
    private Label lblSaldoEsperado;
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
        lblEntradas            = new Label();
        lblSalidas             = new Label();
        lblSaldoEsperado       = new Label();
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
        panelOperaciones.Size = new Size(780, 440);
        panelOperaciones.Visible = false;

        // TabControl dentro del panel operaciones
        tabCaja.Location = new Point(0, 0);
        tabCaja.Size = new Size(780, 435);
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
        grpResumen.Size = new Size(760, 115);
        grpResumen.Parent = tabJornada;

        Font fResumen = new Font("Courier New", 9F);
        Font fResumenBold = new Font("Courier New", 9F, FontStyle.Bold);
        int rx = 10, ry = 20;

        lblMontoInicial.Location  = new Point(rx, ry);       lblMontoInicial.Size  = new Size(340, 18); lblMontoInicial.Font  = fResumen; lblMontoInicial.Parent  = grpResumen;
        lblVentas.Location        = new Point(rx, ry + 18);  lblVentas.Size        = new Size(340, 18); lblVentas.Font        = fResumen; lblVentas.Parent        = grpResumen;
        lblEntradas.Location      = new Point(rx, ry + 36);  lblEntradas.Size      = new Size(340, 18); lblEntradas.Font      = fResumen; lblEntradas.Parent      = grpResumen;
        lblSalidas.Location       = new Point(rx, ry + 54);  lblSalidas.Size       = new Size(340, 18); lblSalidas.Font       = fResumen; lblSalidas.Parent       = grpResumen;
        lblSaldoEsperado.Location = new Point(rx, ry + 72);  lblSaldoEsperado.Size = new Size(340, 18); lblSaldoEsperado.Font = fResumenBold; lblSaldoEsperado.Parent = grpResumen;

        btnCerrarCaja.Location = new Point(610, 35);
        btnCerrarCaja.Size = new Size(130, 45);
        btnCerrarCaja.Text = "Cerrar Caja";
        btnCerrarCaja.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnCerrarCaja.BackColor = Color.Firebrick;
        btnCerrarCaja.ForeColor = Color.White;
        btnCerrarCaja.Click += btnCerrarCaja_Click;
        btnCerrarCaja.Parent = grpResumen;

        grpResumen.Controls.AddRange(new Control[]
        {
            lblMontoInicial, lblVentas, lblEntradas, lblSalidas, lblSaldoEsperado, btnCerrarCaja
        });

        tabJornada.Controls.AddRange(new Control[] { grpMovimiento, dgvMovimientos, grpResumen });

        // ── Tab Historial ────────────────────────────────────────────────
        dgvHistorial.Location = new Point(5, 8);
        dgvHistorial.Size = new Size(760, 390);
        dgvHistorial.ReadOnly = true;
        dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvHistorial.AllowUserToAddRows = false;
        dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvHistorial.Parent = tabHistorial;

        tabHistorial.Controls.Add(dgvHistorial);

        panelOperaciones.Controls.Add(tabCaja);

        // lblMensaje
        lblMensaje.Location = new Point(12, 525);
        lblMensaje.Size = new Size(780, 20);
        lblMensaje.AutoSize = false;

        // Form
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(804, 555);
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
