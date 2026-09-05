using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de administración de categorías: registrar, consultar,
/// modificar y activar/desactivar (sección 5 de los lineamientos).
/// No se permite eliminar categorías, para no perder información
/// relacionada con productos existentes.
/// </summary>
public partial class FormCategorias : Form
{
    private readonly CategoriaRepository _categoriaRepository = new();
    private int _idSeleccionada;

    public FormCategorias()
    {
        InitializeComponent();
        Load += FormCategorias_Load;
    }

    private void FormCategorias_Load(object? sender, EventArgs e)
    {
        CargarCategorias();
    }

    private void CargarCategorias()
    {
        var categorias = _categoriaRepository.ObtenerTodas();

        dgvCategorias.DataSource = null;
        dgvCategorias.DataSource = categorias;

        if (dgvCategorias.Columns["IdCategoria"] != null)
        {
            dgvCategorias.Columns["IdCategoria"].HeaderText = "Id";
            dgvCategorias.Columns["Nombre"].HeaderText = "Nombre";
            dgvCategorias.Columns["Estado"].HeaderText = "Activa";
        }
    }

    private void dgvCategorias_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvCategorias.CurrentRow?.DataBoundItem is not Categoria categoria)
        {
            return;
        }

        _idSeleccionada = categoria.IdCategoria;
        txtNombre.Text = categoria.Nombre;
    }

    private void btnNuevo_Click(object? sender, EventArgs e)
    {
        _idSeleccionada = 0;
        txtNombre.Clear();
        dgvCategorias.ClearSelection();
        txtNombre.Focus();
    }

    private void btnGuardar_Click(object? sender, EventArgs e)
    {
        var nombre = txtNombre.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarMensaje("Debe ingresar el nombre de la categoría.");
            return;
        }

        if (_categoriaRepository.ExisteNombre(nombre, _idSeleccionada))
        {
            MostrarMensaje("Ya existe una categoría con ese nombre.");
            return;
        }

        if (_idSeleccionada == 0)
        {
            _categoriaRepository.Registrar(new Categoria { Nombre = nombre, Estado = true });
            MostrarMensaje("Categoría registrada correctamente.", esError: false);
        }
        else
        {
            var categoria = _categoriaRepository.ObtenerPorId(_idSeleccionada);
            if (categoria is null)
            {
                MostrarMensaje("La categoría seleccionada ya no existe.");
                return;
            }

            categoria.Nombre = nombre;
            _categoriaRepository.Modificar(categoria);
            MostrarMensaje("Categoría modificada correctamente.", esError: false);
        }

        btnNuevo_Click(sender, e);
        CargarCategorias();
    }

    private void btnActivarDesactivar_Click(object? sender, EventArgs e)
    {
        if (_idSeleccionada == 0)
        {
            MostrarMensaje("Seleccione una categoría de la lista.");
            return;
        }

        var categoria = _categoriaRepository.ObtenerPorId(_idSeleccionada);
        if (categoria is null)
        {
            return;
        }

        _categoriaRepository.CambiarEstado(categoria.IdCategoria, !categoria.Estado);
        CargarCategorias();
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
