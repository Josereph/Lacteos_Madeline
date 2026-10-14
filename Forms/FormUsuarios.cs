using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Administración de usuarios: crear cajeros y administradores, cambiar
/// nombre o rol, asignar un PIN nuevo (si alguien lo olvida) y
/// activar/desactivar. Siempre queda al menos un administrador activo.
/// </summary>
public class FormUsuarios : Form
{
    private readonly UsuarioService _usuarioService = new();

    private readonly DataGridView _dgvUsuarios = Construir.Tabla();
    private readonly TextBox _txtNombre = Construir.Texto(40, 240);
    private readonly ComboBox _cboRol = Construir.Lista(240);
    private readonly TextBox _txtPin = Construir.Pin();
    private readonly TextBox _txtConfirmacion = Construir.Pin();
    private readonly Label _lblAyudaPin = Construir.Nota(string.Empty);
    private readonly Label _lblMensaje = Construir.Mensaje();

    private int _idSeleccionado;

    public FormUsuarios()
    {
        Text = "Usuarios";
        BackColor = Tema.BlancoLacteo;
        Font = Tema.FuenteBase;
        ClientSize = new Size(820, 440);

        _cboRol.DataSource = Usuario.Roles.ToList();

        var formulario = Construir.Columna(
            Construir.Etiqueta("Datos del usuario", negrita: true, tamano: 11F),
            Construir.Nota("Administrador: acceso a todo el sistema.\nCajero: punto de venta, ventas realizadas, clientes y caja."),
            Construir.Campos(("Nombre", _txtNombre), ("Rol", _cboRol), ("PIN", _txtPin), ("Repita el PIN", _txtConfirmacion)),
            _lblAyudaPin,
            Construir.Fila(
                Construir.Boton("Nuevo", Tema.RolBoton.Secundario, (_, _) => Limpiar(), 100),
                Construir.Boton("Guardar", Tema.RolBoton.Primario, (_, _) => Guardar(), 110),
                Construir.Boton("Activar / Desactivar", Tema.RolBoton.Secundario, (_, _) => AlternarEstado(), 170)),
            _lblMensaje);
        formulario.Margin = new Padding(20, 0, 0, 0);

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(16, 12, 16, 12) };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        raiz.Controls.Add(_dgvUsuarios, 0, 0);
        raiz.Controls.Add(formulario, 1, 0);
        Controls.Add(raiz);

        _dgvUsuarios.SelectionChanged += (_, _) => MostrarSeleccionado();
        Load += (_, _) => { Cargar(); Limpiar(); };
    }

    private void Cargar()
    {
        _dgvUsuarios.DataSource = null;
        _dgvUsuarios.DataSource = _usuarioService.ListarTodos();
        _dgvUsuarios.OcultarColumna("IdUsuario");
        _dgvUsuarios.Encabezado("Estado", "Activo");
    }

    private void MostrarSeleccionado()
    {
        if (_dgvUsuarios.CurrentRow?.DataBoundItem is not Usuario usuario) return;

        _idSeleccionado = usuario.IdUsuario;
        _txtNombre.Text = usuario.Nombre;
        _cboRol.SelectedItem = usuario.Rol;
        _txtPin.Clear();
        _txtConfirmacion.Clear();
        _lblAyudaPin.Text = "Deje el PIN vacío para conservar el actual, o escriba uno nuevo para cambiarlo.";
    }

    private void Limpiar()
    {
        _idSeleccionado = 0;
        _txtNombre.Clear();
        _cboRol.SelectedItem = Usuario.RolCajero;
        _txtPin.Clear();
        _txtConfirmacion.Clear();
        _lblAyudaPin.Text = $"Usuario nuevo: el PIN es obligatorio ({Pin.LongitudMinima} a {Pin.LongitudMaxima} números).";
        _dgvUsuarios.ClearSelection();
        Construir.MostrarMensaje(_lblMensaje, string.Empty, esError: false);
        _txtNombre.Focus();
    }

    private void Guardar()
    {
        var rol = _cboRol.SelectedItem as string ?? Usuario.RolCajero;
        (bool Exitoso, string Mensaje) resultado;

        if (_idSeleccionado == 0)
        {
            var creado = _usuarioService.Crear(_txtNombre.Text, rol, _txtPin.Text, _txtConfirmacion.Text);
            resultado = (creado.Exitoso, creado.Mensaje);
        }
        else
        {
            resultado = _usuarioService.Modificar(_idSeleccionado, _txtNombre.Text, rol);
            if (resultado.Exitoso && _txtPin.Text.Length > 0)
            {
                var pin = _usuarioService.RestablecerPin(_idSeleccionado, _txtPin.Text, _txtConfirmacion.Text);
                resultado = pin.Exitoso ? (true, "Usuario y PIN actualizados.") : (false, $"Datos guardados, pero el PIN no cambió: {pin.Mensaje}");
            }
        }

        Cargar();
        if (resultado.Exitoso) Limpiar();
        Construir.MostrarMensaje(_lblMensaje, resultado.Mensaje, esError: !resultado.Exitoso);
    }

    private void AlternarEstado()
    {
        if (_idSeleccionado == 0)
        {
            Construir.MostrarMensaje(_lblMensaje, "Seleccione un usuario de la lista.");
            return;
        }

        var resultado = _usuarioService.AlternarEstado(_idSeleccionado);
        Cargar();
        Construir.MostrarMensaje(_lblMensaje, resultado.Mensaje, esError: !resultado.Exitoso);
    }
}
