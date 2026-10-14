using System.Globalization;
using LacteosMadeline.Forms;
using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Helpers;
using LacteosMadeline.Services;

namespace LacteosMadeline;

/// <summary>
/// Ventana principal y única del sistema (shell de pantalla completa).
/// Barra lateral con los módulos, barra superior con estado de caja,
/// alerta de stock crítico y reloj, y un panel central donde se incrustan
/// las pantallas. Ningún módulo se abre como ventana flotante; solo los
/// diálogos breves (cobro en efectivo, confirmaciones, guardar archivo).
/// </summary>
public partial class Form1 : Form
{
    public enum Modulo
    {
        PuntoDeVenta, VentasRealizadas, Clientes, Caja, Productos, Compras, Mermas, Proveedores, Reportes, Administracion
    }

    /// <summary>Módulos que ve un cajero; el administrador ve todos.</summary>
    private static readonly Modulo[] ModulosCajero = { Modulo.PuntoDeVenta, Modulo.VentasRealizadas, Modulo.Clientes, Modulo.Caja };

    private static readonly CultureInfo Cultura = ConfiguracionNegocio.Cultura;

    private readonly Dictionary<Modulo, BotonNavegacion> _botones;
    private readonly CajaService _cajaService = new();
    private readonly InventarioService _inventarioService = new();

    // El punto de venta se conserva entre cambios de módulo para no perder
    // una venta en curso; los demás módulos se crean de nuevo al entrar
    // para mostrar siempre datos actualizados.
    private FormVentas? _formVentas;
    private Control? _vistaActual;
    private Modulo? _moduloActual;
    private string? _ultimoErrorEstado;

    /// <summary>Pide confirmación al salir (se desactiva en pruebas automáticas).</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ConfirmarAlSalir { get; set; } = true;

    /// <summary>El usuario pulsó "Cerrar sesión": el programa vuelve a la pantalla de inicio.</summary>
    public bool CerrarSesionSolicitada { get; private set; }

    public Form1()
    {
        InitializeComponent();

        _botones = new()
        {
            [Modulo.PuntoDeVenta]     = btnNavVentas,
            [Modulo.VentasRealizadas] = btnNavVentasRealizadas,
            [Modulo.Clientes]         = btnNavClientes,
            [Modulo.Caja]             = btnNavCaja,
            [Modulo.Productos]        = btnNavProductos,
            [Modulo.Compras]          = btnNavCompras,
            [Modulo.Mermas]           = btnNavMermas,
            [Modulo.Proveedores]      = btnNavProveedores,
            [Modulo.Reportes]         = btnNavReportes,
            [Modulo.Administracion]   = btnNavAdministracion
        };

        AplicarPermisos();
        lblNegocio.Text = ConfiguracionNegocio.NombreNegocio;
        AjustarFuenteAlAncho(lblNegocio, tamanoMinimo: 10F);

        foreach (var (modulo, boton) in _botones)
        {
            boton.Click += (_, _) => Navegar(modulo);
        }

        Shown += (_, _) =>
        {
            ActualizarReloj();
            ActualizarEstado();
            timerReloj.Start();
            timerEstado.Start();
            Navegar(Modulo.PuntoDeVenta);
        };
        FormClosing += Form1_FormClosing;
    }

    // ── Navegación ──────────────────────────────────────────────────────

    public Modulo? ModuloActual => _moduloActual;

    public Control? VistaActual => _vistaActual;

    /// <summary>Indica si el usuario de la sesión puede abrir el módulo.</summary>
    public static bool PuedeAbrir(Modulo modulo) =>
        Sesion.Actual is null || Sesion.EsAdministrador || ModulosCajero.Contains(modulo);

    /// <summary>
    /// Muestra solo los módulos del rol y el usuario en la barra superior.
    /// </summary>
    private void AplicarPermisos()
    {
        foreach (var (modulo, boton) in _botones)
        {
            boton.Visible = PuedeAbrir(modulo);
        }

        indUsuario.Visible = Sesion.Actual is not null;
        if (Sesion.Actual is { } usuario)
        {
            indUsuario.Actualizar($"{usuario.Nombre}  ·  {usuario.Rol}", Tema.Crema, Tema.Carbon, Tema.GrisCalido);
        }

        btnCerrarSesion.Visible = Sesion.Actual is not null;
    }

    public void Navegar(Modulo modulo)
    {
        if (_moduloActual == modulo && _vistaActual is { IsDisposed: false }) return;
        if (!PuedeAbrir(modulo)) return;

        Cursor = Cursors.WaitCursor;
        panelContent.SuspendLayout();
        try
        {
            CerrarVistaActual();

            Control vista = modulo switch
            {
                Modulo.PuntoDeVenta => _formVentas is { IsDisposed: false } ? _formVentas : (_formVentas = new FormVentas()),
                Modulo.VentasRealizadas => new FormConsultaVentas(),
                Modulo.Clientes => new FormClientes(),
                Modulo.Productos => new VistaSecciones(
                    ("Productos", () => new FormProductos()),
                    ("Categorías", () => new FormCategorias())),
                Modulo.Compras => new VistaSecciones(
                    ("Nueva compra", () => new FormCompras()),
                    ("Consulta de compras", () => new FormConsultaCompras())),
                Modulo.Caja => new FormCaja(),
                Modulo.Mermas => new FormMermas(),
                Modulo.Reportes => new FormReportes(),
                Modulo.Proveedores => new FormProveedores(),
                Modulo.Administracion => new VistaSecciones(
                    ("Usuarios", () => new FormUsuarios()),
                    ("Configuración y respaldos", () => new FormConfiguracion())),
                _ => throw new ArgumentOutOfRangeException(nameof(modulo))
            };

            if (vista is Form formulario)
            {
                if (formulario.Parent == panelContent)
                {
                    formulario.Show();
                    formulario.BringToFront();
                }
                else
                {
                    Navegacion.Incrustar(panelContent, formulario);
                }
            }
            else
            {
                panelContent.Controls.Add(vista);
                vista.BringToFront();
            }

            _vistaActual = vista;
            _moduloActual = modulo;

            foreach (var (m, boton) in _botones) boton.Activo = m == modulo;
            lblModulo.Text = _botones[modulo].Text;
            UbicarIndicadores();
            lblRuta.Text = $"{ConfiguracionNegocio.NombreNegocio}  /  {_botones[modulo].Text}";

            ActualizarEstado();
        }
        finally
        {
            panelContent.ResumeLayout(true);
            Cursor = Cursors.Default;
        }

        // Foco en la primera entrada de la pantalla para operar con teclado.
        _vistaActual?.SelectNextControl(null, true, true, true, true);
    }

    private void CerrarVistaActual()
    {
        if (_vistaActual is null) return;

        if (_vistaActual == _formVentas)
        {
            _formVentas.Hide(); // conserva el carrito
        }
        else
        {
            panelContent.Controls.Remove(_vistaActual);
            _vistaActual.Dispose();
        }

        _vistaActual = null;
    }

    private void MostrarStockCritico()
    {
        if (!PuedeAbrir(Modulo.Reportes)) return;

        Navegar(Modulo.Reportes);
        if (_vistaActual is FormReportes reportes)
        {
            reportes.MostrarPestana(FormReportes.PestanaBajoStock);
        }
    }

    /// <summary>Reduce la letra de una etiqueta hasta que su texto quepa en su ancho.</summary>
    private static void AjustarFuenteAlAncho(Label etiqueta, float tamanoMinimo)
    {
        var tamano = etiqueta.Font.Size;
        while (tamano > tamanoMinimo)
        {
            using var prueba = new Font(etiqueta.Font.FontFamily, tamano, etiqueta.Font.Style);
            if (TextRenderer.MeasureText(etiqueta.Text, prueba).Width <= etiqueta.Width) break;
            tamano -= 0.5F;
        }

        etiqueta.Font = new Font(etiqueta.Font.FontFamily, tamano, etiqueta.Font.Style);
    }

    // ── Barra superior ──────────────────────────────────────────────────

    /// <summary>
    /// Ubica los indicadores a la derecha. En pantallas angostas, si
    /// taparían el título del módulo, se oculta el reloj (Windows ya
    /// muestra la hora en la barra de tareas).
    /// </summary>
    private void UbicarIndicadores()
    {
        var limiteTitulo = Math.Max(lblModulo.Right, lblRuta.Right) + 16;
        var izquierda = panelTopbar.ClientSize.Width - panelIndicadores.Width - 24;

        if (izquierda < limiteTitulo && indReloj.Visible)
        {
            indReloj.Visible = false;
            return; // el cambio de tamaño vuelve a llamar a este método
        }

        if (!indReloj.Visible && izquierda - indReloj.Width - 10 >= limiteTitulo)
        {
            indReloj.Visible = true;
            return;
        }

        panelIndicadores.Location = new Point(izquierda, (panelTopbar.ClientSize.Height - panelIndicadores.Height) / 2);
    }

    private void ActualizarReloj()
    {
        var ahora = DateTime.Now;
        // "lun 12 oct" → "Lun 12 oct" (solo la primera letra en mayúscula).
        var texto = ahora.ToString("ddd d MMM", Cultura).Replace(".", string.Empty);
        var fecha = char.ToUpper(texto[0], Cultura) + texto[1..];
        indReloj.Actualizar($"{fecha}   {ahora:HH:mm}", Tema.Crema, Tema.Carbon, Tema.GrisCalido);
    }

    /// <summary>
    /// Estado de caja y conteo de productos en stock crítico. Son consultas
    /// indexadas de pocos milisegundos; se refrescan cada 5 s y al cambiar
    /// de módulo, así reflejan aperturas, cierres, ventas y compras.
    /// </summary>
    public void ActualizarEstado()
    {
        try
        {
            var caja = _cajaService.ObtenerCajaAbierta();
            if (caja is null)
            {
                indCaja.Actualizar("Caja cerrada", Tema.TerracotaTenue, Tema.TerracotaOscuro, Tema.Terracota);
            }
            else
            {
                indCaja.Actualizar($"Caja abierta  ·  N.º {caja.IdCaja}", ColorTranslator.FromHtml("#E5EEE2"), Tema.Exito, Tema.Exito);
            }

            var criticos = _inventarioService.ConsultarBajoStock().Count;
            if (criticos == 0)
            {
                indStock.Actualizar("Stock en orden", Tema.Crema, Tema.GrisCalido, Tema.GrisCalido);
            }
            else
            {
                indStock.Actualizar(
                    criticos == 1 ? "1 producto en stock crítico" : $"{criticos} productos en stock crítico",
                    Tema.TerracotaTenue, Tema.TerracotaOscuro, Tema.Terracota);
            }
        }
        catch (Exception ex)
        {
            // Un fallo de lectura no debe interrumpir la venta; se registra
            // una sola vez por tipo de error y se reintenta en el siguiente ciclo.
            if (ex.Message != _ultimoErrorEstado)
            {
                _ultimoErrorEstado = ex.Message;
                LogHelper.Error("Actualizar barra de estado", ex);
            }
        }
    }

    // ── Acciones de sistema ─────────────────────────────────────────────

    private void CambiarMiPin()
    {
        if (Sesion.Actual is null) return;

        using var dialogo = new FormCambiarPin();
        dialogo.ShowDialog(this);
    }

    private void btnCerrarSesion_Click(object? sender, EventArgs e)
    {
        var mensaje = _formVentas is { IsDisposed: false, TieneVentaEnCurso: true }
            ? "Hay una venta en curso que no se ha cobrado. Si cierra la sesión, se perderá.\n\n¿Cerrar la sesión?"
            : $"¿Cerrar la sesión de {Sesion.Actual?.Nombre}?";

        if (MessageBox.Show(this, mensaje, "Cerrar sesión", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        CerrarSesionSolicitada = true;
        ConfirmarAlSalir = false;
        Close();
    }

    private void btnSalir_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!ConfirmarAlSalir || e.CloseReason != CloseReason.UserClosing)
        {
            timerReloj.Stop();
            timerEstado.Stop();
            return;
        }

        var mensaje = _formVentas is { IsDisposed: false, TieneVentaEnCurso: true }
            ? "Hay una venta en curso que no se ha cobrado. Si sale ahora, se perderá.\n\n¿Desea salir del sistema?"
            : "¿Desea salir del sistema?";

        var respuesta = MessageBox.Show(this, mensaje, "Salir del sistema",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

        if (respuesta != DialogResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        timerReloj.Stop();
        timerEstado.Stop();
    }
}
