using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Catálogo de clientes: buscar, registrar, modificar y activar/desactivar.
/// Los clientes se asocian (opcionalmente) a las ventas en el punto de venta.
/// </summary>
public class FormClientes : Form
{
    private readonly ClienteService _clienteService = new();

    private readonly TextBox _txtBuscar = Construir.Texto(60, 260, "Buscar por nombre, documento o teléfono…");
    private readonly DataGridView _dgvClientes = Construir.Tabla();
    private readonly TextBox _txtNombre = Construir.Texto(100, 280);
    private readonly TextBox _txtDocumento = Construir.Texto(30, 280, "DUI / NIT");
    private readonly TextBox _txtTelefono = Construir.Texto(30, 280);
    private readonly TextBox _txtDireccion = Construir.Texto(200, 280);
    private readonly TextBox _txtCorreo = Construir.Texto(100, 280);
    private readonly Label _lblMensaje = Construir.Mensaje();

    private int _idSeleccionado;

    public FormClientes()
    {
        Text = "Clientes";
        BackColor = Tema.BlancoLacteo;
        Font = Tema.FuenteBase;
        ClientSize = new Size(820, 440);

        var btnBuscar = Construir.Boton("Buscar", Tema.RolBoton.Secundario, (_, _) => Cargar(), 100);
        _txtBuscar.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Cargar(); } };

        var formulario = Construir.Columna(
            Construir.Etiqueta("Datos del cliente", negrita: true, tamano: 11F),
            Construir.Campos(
                ("Nombre", _txtNombre), ("Documento", _txtDocumento), ("Teléfono", _txtTelefono),
                ("Dirección", _txtDireccion), ("Correo", _txtCorreo)),
            Construir.Fila(
                Construir.Boton("Nuevo", Tema.RolBoton.Secundario, (_, _) => Limpiar(), 100),
                Construir.Boton("Guardar", Tema.RolBoton.Primario, (_, _) => Guardar(), 110),
                Construir.Boton("Activar / Desactivar", Tema.RolBoton.Secundario, (_, _) => AlternarEstado(), 170)),
            _lblMensaje);

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(16, 12, 16, 12) };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        raiz.Controls.Add(Construir.Fila(_txtBuscar, btnBuscar), 0, 0);
        raiz.Controls.Add(_dgvClientes, 0, 1);
        formulario.Margin = new Padding(20, 0, 0, 0);
        raiz.Controls.Add(formulario, 1, 1);
        Controls.Add(raiz);

        _dgvClientes.SelectionChanged += (_, _) => MostrarSeleccionado();
        Load += (_, _) => Cargar();
    }

    private void Cargar()
    {
        _dgvClientes.DataSource = null;
        _dgvClientes.DataSource = _clienteService.Buscar(_txtBuscar.Text.Trim());
        _dgvClientes.OcultarColumna("IdCliente");
        _dgvClientes.Encabezado("Telefono", "Teléfono");
        _dgvClientes.Encabezado("Direccion", "Dirección");
        _dgvClientes.Encabezado("Estado", "Activo");
    }

    private void MostrarSeleccionado()
    {
        if (_dgvClientes.CurrentRow?.DataBoundItem is not Cliente cliente) return;

        _idSeleccionado = cliente.IdCliente;
        _txtNombre.Text = cliente.Nombre;
        _txtDocumento.Text = cliente.Documento ?? string.Empty;
        _txtTelefono.Text = cliente.Telefono ?? string.Empty;
        _txtDireccion.Text = cliente.Direccion ?? string.Empty;
        _txtCorreo.Text = cliente.Correo ?? string.Empty;
    }

    private void Limpiar()
    {
        _idSeleccionado = 0;
        foreach (var texto in new[] { _txtNombre, _txtDocumento, _txtTelefono, _txtDireccion, _txtCorreo }) texto.Clear();
        _dgvClientes.ClearSelection();
        _txtNombre.Focus();
        Construir.MostrarMensaje(_lblMensaje, string.Empty, esError: false);
    }

    private void Guardar()
    {
        var actual = _idSeleccionado == 0 ? null : _clienteService.ObtenerPorId(_idSeleccionado);
        var resultado = _clienteService.Guardar(new Cliente
        {
            IdCliente = _idSeleccionado,
            Nombre = _txtNombre.Text,
            Documento = _txtDocumento.Text,
            Telefono = _txtTelefono.Text,
            Direccion = _txtDireccion.Text,
            Correo = _txtCorreo.Text,
            Estado = actual?.Estado ?? true
        });

        if (!resultado.Exitoso)
        {
            Construir.MostrarMensaje(_lblMensaje, resultado.Mensaje);
            return;
        }

        Limpiar();
        Cargar();
        Construir.MostrarMensaje(_lblMensaje, resultado.Mensaje, esError: false);
    }

    private void AlternarEstado()
    {
        if (_idSeleccionado == 0)
        {
            Construir.MostrarMensaje(_lblMensaje, "Seleccione un cliente de la lista.");
            return;
        }

        var resultado = _clienteService.AlternarEstado(_idSeleccionado);
        Cargar();
        Construir.MostrarMensaje(_lblMensaje, resultado.Mensaje, esError: !resultado.Exitoso);
    }
}
