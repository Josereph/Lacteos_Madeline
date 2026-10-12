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
    public enum Modulo { PuntoDeVenta, Productos, Compras, Caja, Mermas, Reportes, Proveedores }

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");

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

    public Form1()
    {
        InitializeComponent();

        _botones = new()
        {
            [Modulo.PuntoDeVenta] = btnNavVentas,
            [Modulo.Productos]    = btnNavProductos,
            [Modulo.Compras]      = btnNavCompras,
            [Modulo.Caja]         = btnNavCaja,
            [Modulo.Mermas]       = btnNavMermas,
            [Modulo.Reportes]     = btnNavReportes,
            [Modulo.Proveedores]  = btnNavProveedores
        };

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

    public void Navegar(Modulo modulo)
    {
        if (_moduloActual == modulo && _vistaActual is { IsDisposed: false }) return;

        Cursor = Cursors.WaitCursor;
        panelContent.SuspendLayout();
        try
        {
            CerrarVistaActual();

            Control vista = modulo switch
            {
                Modulo.PuntoDeVenta => _formVentas is { IsDisposed: false } ? _formVentas : (_formVentas = new FormVentas()),
                Modulo.Productos => new VistaSecciones(
                    ("Productos", () => new FormProductos()),
                    ("Categorías", () => new FormCategorias())),
                Modulo.Compras => new FormCompras(),
                Modulo.Caja => new FormCaja(),
                Modulo.Mermas => new FormMermas(),
                Modulo.Reportes => new VistaSecciones(
                    ("Reportes", () => new FormReportes()),
                    ("Consulta de ventas", () => new FormConsultaVentas()),
                    ("Consulta de compras", () => new FormConsultaCompras())),
                Modulo.Proveedores => new FormProveedores(),
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
            lblRuta.Text = "Lácteos Madeline  /  " + _botones[modulo].Text;

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
        Navegar(Modulo.Reportes);
        if (_vistaActual is VistaSecciones secciones && secciones.Seccion(0) is FormReportes reportes)
        {
            reportes.MostrarPestana(FormReportes.PestanaBajoStock);
        }
    }

    // ── Barra superior ──────────────────────────────────────────────────

    private void UbicarIndicadores()
    {
        panelIndicadores.Location = new Point(
            panelTopbar.ClientSize.Width - panelIndicadores.Width - 24,
            (panelTopbar.ClientSize.Height - panelIndicadores.Height) / 2);
    }

    private void ActualizarReloj()
    {
        var ahora = DateTime.Now;
        // "lunes, 12 de octubre" → "Lunes, 12 de octubre" (solo la primera letra en mayúscula).
        var texto = ahora.ToString("dddd, d 'de' MMMM", Cultura);
        var fecha = char.ToUpper(texto[0], Cultura) + texto[1..];
        indReloj.Actualizar($"{fecha}   {ahora:HH:mm:ss}", Tema.Crema, Tema.Carbon, Tema.GrisCalido);
        // Ancho fijo para que los segundos no muevan los demás indicadores.
        indReloj.Width = Math.Max(indReloj.Width, 260);
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

    private void btnRespaldo_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Guardar respaldo de base de datos",
            Filter = "Base de datos SQLite (*.db)|*.db",
            FileName = BackupHelper.GenerarNombreArchivo()
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var (exitoso, detalle) = BackupHelper.RealizarBackup(dlg.FileName);

        MessageBox.Show(this, detalle,
            exitoso ? "Respaldo exitoso" : "Error en respaldo",
            MessageBoxButtons.OK,
            exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Error);
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
