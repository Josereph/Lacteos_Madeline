using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de administración de proveedores: registrar, buscar,
/// modificar y activar/desactivar. Se prefiere desactivar antes
/// de eliminar cuando el proveedor tiene compras asociadas
/// (sección 4 y 21 de los lineamientos).
/// </summary>
public partial class FormProveedores : Form
{
    private readonly ProveedorService _proveedorService = new();
    private int _idSeleccionado;

    public FormProveedores()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormProveedores_Load;
    }

    private void FormProveedores_Load(object? sender, EventArgs e)
    {
        CargarProveedores();
    }

    private void CargarProveedores(string filtro = "")
    {
        var proveedores = _proveedorService.Buscar(filtro);

        dgvProveedores.DataSource = null;
        dgvProveedores.DataSource = proveedores;

        if (dgvProveedores.Columns["IdProveedor"] != null)
        {
            dgvProveedores.OcultarColumna("IdProveedor");
            dgvProveedores.Encabezado("Nombre", "Nombre");
            dgvProveedores.Encabezado("Contacto", "Contacto");
            dgvProveedores.Encabezado("Telefono", "Teléfono");
            dgvProveedores.Encabezado("Direccion", "Dirección");
            dgvProveedores.Encabezado("Observaciones", "Observaciones");
            dgvProveedores.Encabezado("Estado", "Activo");
        }
    }

    private void btnBuscar_Click(object? sender, EventArgs e)
    {
        CargarProveedores(txtBuscar.Text.Trim());
    }

    private void dgvProveedores_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvProveedores.CurrentRow?.DataBoundItem is not Proveedor proveedor) return;

        _idSeleccionado       = proveedor.IdProveedor;
        txtNombre.Text        = proveedor.Nombre;
        txtContacto.Text      = proveedor.Contacto ?? string.Empty;
        txtTelefono.Text      = proveedor.Telefono ?? string.Empty;
        txtDireccion.Text     = proveedor.Direccion ?? string.Empty;
        txtObservaciones.Text = proveedor.Observaciones ?? string.Empty;
    }

    private void btnNuevo_Click(object? sender, EventArgs e)
    {
        _idSeleccionado = 0;
        txtNombre.Clear();
        txtContacto.Clear();
        txtTelefono.Clear();
        txtDireccion.Clear();
        txtObservaciones.Clear();
        dgvProveedores.ClearSelection();
        txtNombre.Focus();
        MostrarMensaje(string.Empty);
    }

    private void btnGuardar_Click(object? sender, EventArgs e)
    {
        var proveedor = new Proveedor
        {
            IdProveedor   = _idSeleccionado,
            Nombre        = txtNombre.Text,
            Contacto      = txtContacto.Text,
            Telefono      = txtTelefono.Text,
            Direccion     = txtDireccion.Text,
            Observaciones = txtObservaciones.Text,
            Estado        = true
        };

        var resultado = _proveedorService.Guardar(proveedor);
        if (!resultado.Exitoso)
        {
            MostrarMensaje(resultado.Mensaje);
            return;
        }

        btnNuevo_Click(sender, e);
        CargarProveedores(txtBuscar.Text.Trim());
        MostrarMensaje(resultado.Mensaje, esError: false);
    }

    private void btnActivarDesactivar_Click(object? sender, EventArgs e)
    {
        if (_idSeleccionado == 0)
        {
            MostrarMensaje("Seleccione un proveedor de la lista.");
            return;
        }

        var resultado = _proveedorService.AlternarEstado(_idSeleccionado);
        CargarProveedores(txtBuscar.Text.Trim());
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        lblMensaje.Text = mensaje;
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        Diseno.Anclar(Diseno.Todo, dgvProveedores);
        Diseno.Anclar(Diseno.ArribaDerecha,
            lblNombre, txtNombre, lblContacto, txtContacto, lblTelefono, txtTelefono,
            lblDireccion, txtDireccion, lblObservaciones, txtObservaciones,
            btnNuevo, btnGuardar, btnActivarDesactivar);
        Diseno.Anclar(Diseno.AbajoAncho, lblMensaje);
    }
}
