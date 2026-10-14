using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de administración de categorías: registrar, consultar,
/// modificar y activar/desactivar (sección 5 de los lineamientos).
/// No se permite eliminar categorías, para no perder información
/// relacionada con productos existentes.
/// </summary>
public partial class FormCategorias : Form
{
    private readonly CategoriaService _categoriaService = new();
    private int _idSeleccionada;

    public FormCategorias()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormCategorias_Load;
    }

    private void FormCategorias_Load(object? sender, EventArgs e)
    {
        CargarCategorias();
    }

    private void CargarCategorias()
    {
        var categorias = _categoriaService.ObtenerTodas();

        dgvCategorias.DataSource = null;
        dgvCategorias.DataSource = categorias;

        if (dgvCategorias.Columns["IdCategoria"] != null)
        {
            dgvCategorias.Encabezado("IdCategoria", "Id");
            dgvCategorias.Encabezado("Nombre", "Nombre");
            dgvCategorias.Encabezado("Estado", "Activa");
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
        var resultado = _categoriaService.Guardar(_idSeleccionada, txtNombre.Text);
        if (!resultado.Exitoso)
        {
            MostrarMensaje(resultado.Mensaje);
            return;
        }

        btnNuevo_Click(sender, e);
        CargarCategorias();
        MostrarMensaje(resultado.Mensaje, esError: false);
    }

    private void btnActivarDesactivar_Click(object? sender, EventArgs e)
    {
        if (_idSeleccionada == 0)
        {
            MostrarMensaje("Seleccione una categoría de la lista.");
            return;
        }

        var resultado = _categoriaService.AlternarEstado(_idSeleccionada);
        CargarCategorias();
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
        Diseno.Anclar(Diseno.Todo, dgvCategorias);
        Diseno.Anclar(Diseno.AbajoIzquierda, lblNombre, txtNombre, btnNuevo, btnGuardar, btnActivarDesactivar);
        Diseno.Anclar(Diseno.AbajoAncho, lblMensaje);
    }
}
