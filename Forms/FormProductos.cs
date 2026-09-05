using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de administración de productos: registrar, buscar, consultar,
/// modificar, asociar categoría y activar/desactivar (sección 6 de los
/// lineamientos).
/// </summary>
public partial class FormProductos : Form
{
    private readonly ProductoService _productoService = new();
    private readonly CategoriaRepository _categoriaRepository = new();

    private int _idSeleccionado;

    public FormProductos()
    {
        InitializeComponent();
        Load += FormProductos_Load;
    }

    private void FormProductos_Load(object? sender, EventArgs e)
    {
        CargarCategorias();
        CargarProductos();
    }

    private void CargarCategorias()
    {
        var categorias = _categoriaRepository.ObtenerTodas(incluirInactivas: false);
        cboCategoria.DisplayMember = "Nombre";
        cboCategoria.ValueMember = "IdCategoria";
        cboCategoria.DataSource = categorias;
    }

    private void CargarProductos(string filtro = "")
    {
        var productos = _productoService.Buscar(filtro);

        dgvProductos.DataSource = null;
        dgvProductos.DataSource = productos;

        if (dgvProductos.Columns["IdCategoria"] != null)
        {
            dgvProductos.Columns["IdCategoria"].Visible = false;
            dgvProductos.Columns["NombreCategoria"].HeaderText = "Categoría";
            dgvProductos.Columns["PrecioCompra"].HeaderText = "P. Compra";
            dgvProductos.Columns["PrecioVenta"].HeaderText = "P. Venta";
            dgvProductos.Columns["Existencia"].HeaderText = "Existencia";
            dgvProductos.Columns["StockMinimo"].HeaderText = "Stock Mín.";
            dgvProductos.Columns["Estado"].HeaderText = "Activo";
        }
    }

    private void btnBuscar_Click(object? sender, EventArgs e)
    {
        CargarProductos(txtBuscar.Text.Trim());
    }

    private void dgvProductos_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvProductos.CurrentRow?.DataBoundItem is not Producto producto)
        {
            return;
        }

        _idSeleccionado = producto.IdProducto;
        txtNombre.Text = producto.Nombre;
        cboCategoria.SelectedValue = producto.IdCategoria;
        txtPrecioCompra.Text = producto.PrecioCompra?.ToString() ?? string.Empty;
        txtPrecioVenta.Text = producto.PrecioVenta.ToString();
        txtExistencia.Text = producto.Existencia.ToString();
        txtStockMinimo.Text = producto.StockMinimo.ToString();
    }

    private void btnNuevo_Click(object? sender, EventArgs e)
    {
        _idSeleccionado = 0;
        txtNombre.Clear();
        txtPrecioCompra.Clear();
        txtPrecioVenta.Clear();
        txtExistencia.Clear();
        txtStockMinimo.Clear();
        dgvProductos.ClearSelection();
        txtNombre.Focus();
    }

    private void btnGuardar_Click(object? sender, EventArgs e)
    {
        if (cboCategoria.SelectedValue is not int idCategoria)
        {
            MostrarMensaje("Debe registrar al menos una categoría antes de agregar productos.");
            return;
        }

        decimal? precioCompra = null;
        if (!string.IsNullOrWhiteSpace(txtPrecioCompra.Text))
        {
            if (!decimal.TryParse(txtPrecioCompra.Text, out var valorPrecioCompra))
            {
                MostrarMensaje("El precio de compra ingresado no es válido.");
                return;
            }
            precioCompra = valorPrecioCompra;
        }

        if (!decimal.TryParse(txtPrecioVenta.Text, out var precioVenta))
        {
            MostrarMensaje("El precio de venta ingresado no es válido.");
            return;
        }

        if (!int.TryParse(txtExistencia.Text, out var existencia))
        {
            MostrarMensaje("La existencia ingresada no es válida.");
            return;
        }

        if (!int.TryParse(txtStockMinimo.Text, out var stockMinimo))
        {
            stockMinimo = 0;
        }

        var producto = new Producto
        {
            IdProducto = _idSeleccionado,
            Nombre = txtNombre.Text.Trim(),
            IdCategoria = idCategoria,
            PrecioCompra = precioCompra,
            PrecioVenta = precioVenta,
            Existencia = existencia,
            StockMinimo = stockMinimo,
            Estado = true
        };

        (bool Exitoso, string Mensaje) resultado;

        if (_idSeleccionado == 0)
        {
            var registro = _productoService.Registrar(producto);
            resultado = (registro.Exitoso, registro.Mensaje);
        }
        else
        {
            resultado = _productoService.Modificar(producto);
        }

        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            btnNuevo_Click(sender, e);
            CargarProductos(txtBuscar.Text.Trim());
        }
    }

    private void btnActivarDesactivar_Click(object? sender, EventArgs e)
    {
        if (_idSeleccionado == 0)
        {
            MostrarMensaje("Seleccione un producto de la lista.");
            return;
        }

        var producto = _productoService.ObtenerPorId(_idSeleccionado);
        if (producto is null)
        {
            return;
        }

        _productoService.CambiarEstado(producto.IdProducto, !producto.Estado);
        CargarProductos(txtBuscar.Text.Trim());
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
