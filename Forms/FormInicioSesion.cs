using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Inicio de sesión: el usuario elige su nombre y escribe su PIN. Tras
/// 5 intentos fallidos seguidos, la pantalla se bloquea 30 segundos para
/// impedir que se adivine un PIN probando combinaciones.
/// </summary>
public class FormInicioSesion : DialogoBase
{
    private const int IntentosPermitidos = 5;
    private static readonly TimeSpan Bloqueo = TimeSpan.FromSeconds(30);

    private readonly UsuarioService _usuarioService = new();
    private readonly ListBox _lstUsuarios = new();
    private readonly TextBox _txtPin = Construir.Pin();
    private readonly Button _btnEntrar;
    private readonly System.Windows.Forms.Timer _temporizador = new() { Interval = 1000 };

    private int _fallos;
    private DateTime _bloqueadoHasta = DateTime.MinValue;

    public Usuario? UsuarioAutenticado { get; private set; }

    public FormInicioSesion() : base($"{ConfiguracionNegocio.NombreNegocio} – Iniciar sesión")
    {
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        Icon = SystemIcons.Application;

        _lstUsuarios.Width = 320;
        _lstUsuarios.Height = 190;
        _lstUsuarios.Font = new Font(Tema.Familia, 13F);
        _lstUsuarios.IntegralHeight = false;
        _lstUsuarios.Margin = new Padding(0, 4, 0, 8);
        _lstUsuarios.DataSource = _usuarioService.ListarActivos();
        _lstUsuarios.Format += (_, e) =>
        {
            if (e.ListItem is Usuario u) e.Value = $"{u.Nombre}   ({u.Rol})";
        };
        _lstUsuarios.DoubleClick += (_, _) => _txtPin.Focus();

        _txtPin.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsAsciiDigit(e.KeyChar)) e.Handled = true;
        };

        _btnEntrar = Construir.Boton("Entrar", Tema.RolBoton.Primario, (_, _) => Entrar(), 150);
        var btnSalir = Construir.Boton("Salir", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110);

        Contenido.Controls.AddRange(new Control[]
        {
            Construir.Titulo(ConfiguracionNegocio.NombreNegocio),
            Construir.Nota("Seleccione su usuario y escriba su PIN."),
            _lstUsuarios,
            Construir.Etiqueta("PIN", negrita: true),
            _txtPin
        });
        Botones.Controls.AddRange(new Control[] { _btnEntrar, btnSalir });

        AcceptButton = _btnEntrar;
        _temporizador.Tick += (_, _) => ActualizarBloqueo();
        Shown += (_, _) => { if (_lstUsuarios.Items.Count == 1) _txtPin.Focus(); else _lstUsuarios.Focus(); };
    }

    private void Entrar()
    {
        if (DateTime.Now < _bloqueadoHasta) return;

        if (_lstUsuarios.SelectedItem is not Usuario usuario)
        {
            MostrarError("Seleccione su usuario.");
            return;
        }

        var resultado = _usuarioService.Autenticar(usuario.IdUsuario, _txtPin.Text);
        if (resultado.Exitoso && resultado.Usuario is not null)
        {
            UsuarioAutenticado = resultado.Usuario;
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _txtPin.Clear();
        _txtPin.Focus();
        _fallos++;

        if (_fallos >= IntentosPermitidos)
        {
            _fallos = 0;
            _bloqueadoHasta = DateTime.Now + Bloqueo;
            _btnEntrar.Enabled = false;
            _txtPin.Enabled = false;
            _temporizador.Start();
            ActualizarBloqueo();
            return;
        }

        MostrarError($"{resultado.Mensaje} Intentos restantes: {IntentosPermitidos - _fallos}.");
    }

    private void ActualizarBloqueo()
    {
        var restante = _bloqueadoHasta - DateTime.Now;
        if (restante <= TimeSpan.Zero)
        {
            _temporizador.Stop();
            _btnEntrar.Enabled = true;
            _txtPin.Enabled = true;
            _txtPin.Focus();
            MostrarError(string.Empty);
            return;
        }

        MostrarError($"Demasiados intentos fallidos. Espere {Math.Ceiling(restante.TotalSeconds)} segundos.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _temporizador.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// Primer arranque: no hay usuarios, se crea el administrador.
/// </summary>
public class FormPrimerAdministrador : DialogoBase
{
    private readonly UsuarioService _usuarioService = new();
    private readonly TextBox _txtNombre = Construir.Texto(40, 260, "Ej.: Madeline");
    private readonly TextBox _txtPin = Construir.Pin();
    private readonly TextBox _txtConfirmacion = Construir.Pin();

    public Usuario? Administrador { get; private set; }

    public FormPrimerAdministrador() : base("Configuración inicial")
    {
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;

        Contenido.Controls.AddRange(new Control[]
        {
            Construir.Titulo("Bienvenido"),
            Construir.Nota("Es la primera vez que se usa el sistema en esta base de datos. Cree el usuario administrador: " +
                           $"podrá crear después a los cajeros. El PIN debe tener de {Pin.LongitudMinima} a {Pin.LongitudMaxima} números; " +
                           "no lo olvide."),
            Construir.Campos(("Nombre", _txtNombre), ("PIN", _txtPin), ("Repita el PIN", _txtConfirmacion))
        });

        var btnCrear = Construir.Boton("Crear y entrar", Tema.RolBoton.Primario, (_, _) => Crear(), 160);
        Botones.Controls.AddRange(new Control[]
        {
            btnCrear,
            Construir.Boton("Salir", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110)
        });
        AcceptButton = btnCrear;
    }

    private void Crear()
    {
        var resultado = _usuarioService.CrearPrimerAdministrador(_txtNombre.Text, _txtPin.Text, _txtConfirmacion.Text);
        if (!resultado.Exitoso || resultado.Usuario is null)
        {
            MostrarError(resultado.Mensaje);
            return;
        }

        Administrador = resultado.Usuario;
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>
/// Un administrador autoriza con su PIN una acción de un cajero (anular
/// una venta, aplicar un descuento).
/// </summary>
public class FormAutorizacion : DialogoBase
{
    private readonly UsuarioService _usuarioService = new();
    private readonly ComboBox _cboAdministrador = Construir.Lista();
    private readonly TextBox _txtPin = Construir.Pin();

    public Usuario? Administrador { get; private set; }

    public FormAutorizacion(string accion) : base("Autorización requerida")
    {
        _cboAdministrador.DataSource = _usuarioService.ListarAdministradoresActivos();

        Contenido.Controls.AddRange(new Control[]
        {
            Construir.Titulo("Autorización"),
            Construir.Nota($"{accion} requiere la autorización de un administrador."),
            Construir.Campos(("Administrador", _cboAdministrador), ("PIN", _txtPin))
        });

        var btnAutorizar = Construir.Boton("Autorizar", Tema.RolBoton.Primario, (_, _) => Autorizar());
        Botones.Controls.AddRange(new Control[]
        {
            btnAutorizar,
            Construir.Boton("Cancelar", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110)
        });
        AcceptButton = btnAutorizar;
        Shown += (_, _) => _txtPin.Focus();
    }

    private void Autorizar()
    {
        if (_cboAdministrador.SelectedItem is not Usuario administrador)
        {
            MostrarError("No hay administradores activos.");
            return;
        }

        var resultado = _usuarioService.Autorizar(administrador.IdUsuario, _txtPin.Text);
        if (!resultado.Exitoso)
        {
            _txtPin.Clear();
            MostrarError(resultado.Mensaje);
            return;
        }

        Administrador = resultado.Administrador;
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// Si el usuario actual es administrador no pide nada. Si es cajero,
    /// pide el PIN de un administrador. Devuelve si se puede continuar y,
    /// en su caso, quién autorizó.
    /// </summary>
    public static (bool Autorizado, int? IdAutoriza) Solicitar(IWin32Window? dueno, string accion)
    {
        if (Sesion.Actual is null || Sesion.EsAdministrador)
        {
            return (true, null);
        }

        using var dialogo = new FormAutorizacion(accion);
        return dialogo.ShowDialog(dueno) == DialogResult.OK && dialogo.Administrador is not null
            ? (true, dialogo.Administrador.IdUsuario)
            : (false, null);
    }
}

/// <summary>Cada usuario cambia su propio PIN.</summary>
public class FormCambiarPin : DialogoBase
{
    private readonly UsuarioService _usuarioService = new();
    private readonly TextBox _txtActual = Construir.Pin();
    private readonly TextBox _txtNuevo = Construir.Pin();
    private readonly TextBox _txtConfirmacion = Construir.Pin();

    public FormCambiarPin() : base("Cambiar mi PIN")
    {
        Contenido.Controls.AddRange(new Control[]
        {
            Construir.Titulo(Sesion.Actual?.Nombre ?? "Cambiar PIN"),
            Construir.Campos(("PIN actual", _txtActual), ("PIN nuevo", _txtNuevo), ("Repita el PIN nuevo", _txtConfirmacion))
        });

        var btnGuardar = Construir.Boton("Guardar", Tema.RolBoton.Primario, (_, _) => Guardar());
        Botones.Controls.AddRange(new Control[]
        {
            btnGuardar,
            Construir.Boton("Cancelar", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110)
        });
        AcceptButton = btnGuardar;
    }

    private void Guardar()
    {
        if (Sesion.Actual is null) return;

        var resultado = _usuarioService.CambiarPropioPin(Sesion.Actual.IdUsuario, _txtActual.Text, _txtNuevo.Text, _txtConfirmacion.Text);
        if (!resultado.Exitoso)
        {
            MostrarError(resultado.Mensaje);
            return;
        }

        MessageBox.Show(this, resultado.Mensaje, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>Pide un texto (por ejemplo, el motivo de una anulación).</summary>
public class FormEntradaTexto : DialogoBase
{
    private readonly TextBox _txtTexto;

    public string Texto => _txtTexto.Text.Trim();

    public FormEntradaTexto(string titulo, string mensaje, int maxLength = 200) : base(titulo)
    {
        _txtTexto = Construir.Texto(maxLength, 380);
        _txtTexto.Multiline = true;
        _txtTexto.Height = 70;

        Contenido.Controls.AddRange(new Control[] { Construir.Nota(mensaje), _txtTexto });

        var btnAceptar = Construir.Boton("Aceptar", Tema.RolBoton.Primario, (_, _) =>
        {
            if (Texto.Length == 0)
            {
                MostrarError("Escriba el texto solicitado.");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        });
        Botones.Controls.AddRange(new Control[]
        {
            btnAceptar,
            Construir.Boton("Cancelar", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110)
        });
    }
}
