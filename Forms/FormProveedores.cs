using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de administración de proveedores: registrar, buscar,
/// modificar y activar/desactivar. Se prefiere desactivar antes
/// de eliminar cuando el proveedor tiene compras asociadas
/// (sección 4 y 21 de los lineamientos).
/// </summary>
public partial class FormProveedores : Form
{
    private readonly ProveedorRepository _proveedorRepository = new();
    private int _idSeleccionado;

    public FormProveedores()
    {
        InitializeComponent();
        Load += FormProveedores_Load;
    }

    private void FormProveedores_Load(object? sender, EventArgs e)
    {
        CargarProveedores();
    }

    private void CargarProveedores(string filtro = "")
    {
        var proveedores = _proveedorRepository.Buscar(filtro);

        dgvProveedores.DataSource = null;
        dgvProveedores.DataSource = proveedores;

        if (dgvProveedores.Columns["IdProveedor"] != null)
        {
            dgvProveedores.Columns["IdProveedor"].Visible = false;
            dgvProveedores.Columns["Nombre"].HeaderText = "Nombre";
            dgvProveedores.Columns["Contacto"].HeaderText = "Contacto";
            dgvProveedores.Columns["Telefono"].HeaderText = "Teléfono";
            dgvProveedores.Columns["Direccion"].HeaderText = "Dirección";
            dgvProveedores.Columns["Observaciones"].HeaderText = "Observaciones";
            dgvProveedores.Columns["Estado"].HeaderText = "Activo";
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
        var nombre = txtNombre.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarMensaje("El nombre del proveedor es obligatorio.");
            return;
        }

        var proveedor = new Proveedor
        {
            IdProveedor   = _idSeleccionado,
            Nombre        = nombre,
            Contacto      = string.IsNullOrWhiteSpace(txtContacto.Text) ? null : txtContacto.Text.Trim(),
            Telefono      = string.IsNullOrWhiteSpace(txtTelefono.Text) ? null : txtTelefono.Text.Trim(),
            Direccion     = string.IsNullOrWhiteSpace(txtDireccion.Text) ? null : txtDireccion.Text.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(txtObservaciones.Text) ? null : txtObservaciones.Text.Trim(),
            Estado        = true
        };

        try
        {
            if (_idSeleccionado == 0)
            {
                _proveedorRepository.Registrar(proveedor);
                MostrarMensaje("Proveedor registrado correctamente.", esError: false);
            }
            else
            {
                _proveedorRepository.Modificar(proveedor);
                MostrarMensaje("Proveedor modificado correctamente.", esError: false);
            }

            btnNuevo_Click(sender, e);
            CargarProveedores(txtBuscar.Text.Trim());
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al guardar: {ex.Message}");
        }
    }

    private void btnActivarDesactivar_Click(object? sender, EventArgs e)
    {
        if (_idSeleccionado == 0)
        {
            MostrarMensaje("Seleccione un proveedor de la lista.");
            return;
        }

        var proveedor = _proveedorRepository.ObtenerPorId(_idSeleccionado);
        if (proveedor is null) return;

        _proveedorRepository.CambiarEstado(_idSeleccionado, !proveedor.Estado);
        CargarProveedores(txtBuscar.Text.Trim());
        MostrarMensaje($"Proveedor {(proveedor.Estado ? "desactivado" : "activado")} correctamente.", esError: false);
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
