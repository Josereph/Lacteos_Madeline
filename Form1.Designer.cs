using LacteosMadeline.Forms;
using LacteosMadeline.Forms.Shell;

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

    // Barra lateral
    private Panel panelSidebar;
    private Panel panelMarca;
    private Label lblMarca;
    private Label lblNegocio;
    private Label lblDescripcion;
    private Panel panelNavegacion;
    private Label lblSeccionModulos;
    private BotonNavegacion btnNavVentas;
    private BotonNavegacion btnNavProductos;
    private BotonNavegacion btnNavCompras;
    private BotonNavegacion btnNavCaja;
    private BotonNavegacion btnNavMermas;
    private BotonNavegacion btnNavReportes;
    private BotonNavegacion btnNavProveedores;
    private BotonNavegacion btnNavVentasRealizadas;
    private BotonNavegacion btnNavClientes;
    private BotonNavegacion btnNavAdministracion;
    private Panel panelPieSidebar;
    private BotonNavegacion btnCerrarSesion;
    private BotonNavegacion btnSalir;

    // Barra superior
    private Panel panelTopbar;
    private Label lblModulo;
    private Label lblRuta;
    private FlowLayoutPanel panelIndicadores;
    private IndicadorEstado indUsuario;
    private IndicadorEstado indCaja;
    private IndicadorEstado indStock;
    private IndicadorEstado indReloj;

    // Contenido
    private PanelSinParpadeo panelContent;

    private System.Windows.Forms.Timer timerReloj;
    private System.Windows.Forms.Timer timerEstado;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        panelSidebar = new Panel();
        panelMarca = new Panel();
        lblMarca = new Label();
        lblNegocio = new Label();
        lblDescripcion = new Label();
        panelNavegacion = new Panel();
        lblSeccionModulos = new Label();
        btnNavVentas = new BotonNavegacion();
        btnNavProductos = new BotonNavegacion();
        btnNavCompras = new BotonNavegacion();
        btnNavCaja = new BotonNavegacion();
        btnNavMermas = new BotonNavegacion();
        btnNavReportes = new BotonNavegacion();
        btnNavProveedores = new BotonNavegacion();
        btnNavVentasRealizadas = new BotonNavegacion();
        btnNavClientes = new BotonNavegacion();
        btnNavAdministracion = new BotonNavegacion();
        panelPieSidebar = new Panel();
        btnCerrarSesion = new BotonNavegacion();
        btnSalir = new BotonNavegacion();
        panelTopbar = new Panel();
        lblModulo = new Label();
        lblRuta = new Label();
        panelIndicadores = new FlowLayoutPanel();
        indUsuario = new IndicadorEstado();
        indCaja = new IndicadorEstado();
        indStock = new IndicadorEstado();
        indReloj = new IndicadorEstado();
        panelContent = new PanelSinParpadeo();
        timerReloj = new System.Windows.Forms.Timer(components);
        timerEstado = new System.Windows.Forms.Timer(components);

        panelSidebar.SuspendLayout();
        panelNavegacion.SuspendLayout();
        panelTopbar.SuspendLayout();
        SuspendLayout();

        var crema = Tema.Crema;
        var cremaTenue = ColorTranslator.FromHtml("#A9A29A");

        // ── Barra lateral ───────────────────────────────────────────────
        panelSidebar.Dock = DockStyle.Left;
        panelSidebar.Width = 240;
        panelSidebar.BackColor = Tema.Carbon;

        // Encabezado institucional
        panelMarca.Dock = DockStyle.Top;
        panelMarca.Height = 118;
        panelMarca.BackColor = Tema.Carbon;
        panelMarca.Paint += (_, e) =>
        {
            using var linea = new Pen(ColorTranslator.FromHtml("#3A3433"));
            e.Graphics.DrawLine(linea, 24, panelMarca.Height - 1, panelMarca.Width - 24, panelMarca.Height - 1);
        };

        lblMarca.AutoSize = false;
        lblMarca.Location = new Point(24, 24);
        lblMarca.Size = new Size(200, 18);
        lblMarca.Font = new Font(Tema.Familia, 9F, FontStyle.Bold);
        lblMarca.ForeColor = Tema.CafeClaro;
        lblMarca.Text = "SYSCAT";

        lblNegocio.AutoSize = false;
        lblNegocio.Location = new Point(22, 44);
        lblNegocio.Size = new Size(210, 32);
        lblNegocio.Font = new Font(Tema.Familia, 16F, FontStyle.Bold);
        lblNegocio.ForeColor = crema;
        lblNegocio.Text = "Lácteos Madeline";

        lblDescripcion.AutoSize = false;
        lblDescripcion.Location = new Point(24, 78);
        lblDescripcion.Size = new Size(200, 20);
        lblDescripcion.Font = new Font(Tema.Familia, 8.75F);
        lblDescripcion.ForeColor = cremaTenue;
        lblDescripcion.Text = "Facturación e inventario";

        panelMarca.Controls.AddRange(new Control[] { lblMarca, lblNegocio, lblDescripcion });

        // Navegación (los controles con Dock=Top se apilan en orden inverso)
        panelNavegacion.Dock = DockStyle.Fill;
        panelNavegacion.BackColor = Tema.Carbon;
        panelNavegacion.Padding = new Padding(0, 4, 0, 0);
        panelNavegacion.AutoScroll = true; // por si la pantalla es muy baja

        lblSeccionModulos.Dock = DockStyle.Top;
        lblSeccionModulos.Height = 28;
        lblSeccionModulos.Padding = new Padding(24, 12, 0, 0);
        lblSeccionModulos.Font = new Font(Tema.Familia, 8.25F, FontStyle.Bold);
        lblSeccionModulos.ForeColor = cremaTenue;
        lblSeccionModulos.Text = "MÓDULOS";

        ConfigurarNav(btnNavVentas,           "Punto de Venta",          Icono.PuntoDeVenta);
        ConfigurarNav(btnNavVentasRealizadas, "Ventas Realizadas",       Icono.Recibo);
        ConfigurarNav(btnNavClientes,         "Clientes",                Icono.Cliente);
        ConfigurarNav(btnNavCaja,             "Control de Caja",         Icono.Caja);
        ConfigurarNav(btnNavProductos,        "Productos e Inventario",  Icono.Productos);
        ConfigurarNav(btnNavCompras,          "Registro de Compras",     Icono.Compras);
        ConfigurarNav(btnNavMermas,           "Mermas y Bajas",          Icono.Mermas);
        ConfigurarNav(btnNavProveedores,      "Proveedores",             Icono.Proveedores);
        ConfigurarNav(btnNavReportes,         "Reportes",                Icono.Reportes);
        ConfigurarNav(btnNavAdministracion,   "Administración",          Icono.Engrane);

        panelNavegacion.Controls.AddRange(new Control[]
        {
            btnNavAdministracion, btnNavReportes, btnNavProveedores, btnNavMermas, btnNavCompras,
            btnNavProductos, btnNavCaja, btnNavClientes, btnNavVentasRealizadas, btnNavVentas, lblSeccionModulos
        });

        // Pie: acciones de sistema delimitadas
        panelPieSidebar.Dock = DockStyle.Bottom;
        panelPieSidebar.Height = 110;
        panelPieSidebar.BackColor = Tema.Carbon;
        panelPieSidebar.Padding = new Padding(0, 6, 0, 14);
        panelPieSidebar.Paint += (_, e) =>
        {
            using var linea = new Pen(ColorTranslator.FromHtml("#3A3433"));
            e.Graphics.DrawLine(linea, 24, 0, panelPieSidebar.Width - 24, 0);
        };

        ConfigurarNav(btnCerrarSesion, "Cerrar sesión", Icono.Candado);
        btnCerrarSesion.EsAccionSistema = true;
        btnCerrarSesion.Height = 44;
        btnCerrarSesion.Click += btnCerrarSesion_Click;

        ConfigurarNav(btnSalir, "Salir del sistema", Icono.Salir);
        btnSalir.EsAccionSistema = true;
        btnSalir.Height = 44;
        btnSalir.Click += btnSalir_Click;

        panelPieSidebar.Controls.AddRange(new Control[] { btnSalir, btnCerrarSesion });

        panelSidebar.Controls.Add(panelNavegacion);
        panelSidebar.Controls.Add(panelPieSidebar);
        panelSidebar.Controls.Add(panelMarca);

        // ── Barra superior ──────────────────────────────────────────────
        panelTopbar.Dock = DockStyle.Top;
        panelTopbar.Height = 72;
        panelTopbar.BackColor = Color.White;
        panelTopbar.Paint += (_, e) =>
        {
            using var linea = new Pen(Tema.BordeSuave);
            e.Graphics.DrawLine(linea, 0, panelTopbar.Height - 1, panelTopbar.Width, panelTopbar.Height - 1);
        };

        lblRuta.AutoSize = true;
        lblRuta.Location = new Point(28, 12);
        lblRuta.Font = new Font(Tema.Familia, 8.75F);
        lblRuta.ForeColor = Tema.GrisCalido;
        lblRuta.Text = "Lácteos Madeline";

        lblModulo.AutoSize = true;
        lblModulo.Location = new Point(26, 30);
        lblModulo.Font = new Font(Tema.Familia, 16F, FontStyle.Bold);
        lblModulo.ForeColor = Tema.Carbon;
        lblModulo.Text = "Punto de Venta";

        panelIndicadores.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        panelIndicadores.FlowDirection = FlowDirection.LeftToRight;
        panelIndicadores.WrapContents = false;
        panelIndicadores.AutoSize = true;
        panelIndicadores.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panelIndicadores.BackColor = Color.White;
        panelIndicadores.Location = new Point(600, 18);

        indUsuario.Icono = Icono.Usuario;
        indUsuario.Clicable = true;
        indUsuario.Margin = new Padding(0, 0, 10, 0);
        indUsuario.Click += (_, _) => CambiarMiPin();

        indCaja.MostrarPunto = true;
        indCaja.Clicable = true;
        indCaja.Margin = new Padding(0, 0, 10, 0);
        indCaja.Click += (_, _) => Navegar(Modulo.Caja);

        indStock.Icono = Icono.Alerta;
        indStock.Clicable = true;
        indStock.Margin = new Padding(0, 0, 10, 0);
        indStock.Click += (_, _) => MostrarStockCritico();

        indReloj.Icono = Icono.Reloj;
        indReloj.Margin = new Padding(0);
        indReloj.Font = new Font(Tema.Familia, 9.75F);

        panelIndicadores.Controls.AddRange(new Control[] { indUsuario, indCaja, indStock, indReloj });
        panelTopbar.Controls.AddRange(new Control[] { lblRuta, lblModulo, panelIndicadores });
        panelTopbar.Resize += (_, _) => UbicarIndicadores();
        panelIndicadores.SizeChanged += (_, _) => UbicarIndicadores();

        // ── Contenido ───────────────────────────────────────────────────
        panelContent.Dock = DockStyle.Fill;
        panelContent.BackColor = Tema.BlancoLacteo;

        // ── Temporizadores ──────────────────────────────────────────────
        timerReloj.Interval = 1000;
        timerReloj.Tick += (_, _) => ActualizarReloj();
        timerEstado.Interval = 5000;
        timerEstado.Tick += (_, _) => ActualizarEstado();

        // ── Form ────────────────────────────────────────────────────────
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1366, 768);
        MinimumSize = new Size(1280, 720);
        Controls.Add(panelContent);
        Controls.Add(panelTopbar);
        Controls.Add(panelSidebar);
        Text = "SYSCAT – Lácteos Madeline";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        panelSidebar.ResumeLayout(false);
        panelNavegacion.ResumeLayout(false);
        panelTopbar.ResumeLayout(false);
        panelTopbar.PerformLayout();
        ResumeLayout(false);
    }

    private static void ConfigurarNav(BotonNavegacion boton, string texto, Icono icono)
    {
        boton.Text = texto;
        boton.Icono = icono;
        boton.Dock = DockStyle.Top;
        boton.Height = 42;
    }

    #endregion
}
