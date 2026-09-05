using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de registro de compras: seleccionar proveedor, agregar
/// productos con cantidad y costo, confirmar y actualizar inventario
/// automáticamente (sección 5 y 7 de los lineamientos).
/// </summary>
public partial class FormCompras : Form
{
    private readonly CompraService _compraService = new();
    private readonly ProveedorRepository _proveedorRepository = new();
    private readonly ProductoRepository _productoRepository = new();

    public FormCompras()
    {
        InitializeComponent();
        Load += FormCompras_Load;
    }

    private void FormCompras_Load(object? sender, EventArgs e)
    {
        _compraService.IniciarCompra();
        CargarProveedores();
        CargarProductos();
        ActualizarDetalle();
    }

    private void CargarProveedores()
    {
        var proveedores = _proveedorRepository.Buscar(soloActivos: true);
        cboProveedor.DisplayMember = "Nombre";
        cboProveedor.ValueMember = "IdProveedor";
        cboProveedor.DataSource = proveedores;
    }

    private void CargarProductos()
    {
        var productos = _productoRepository.Buscar(soloActivos: true);
        cboProducto.DisplayMember = "Nombre";
        cboProducto.ValueMember = "IdProducto";
        cboProducto.DataSource = productos;
    }

    private void btnAgregar_Click(object? sender, EventArgs e)
    {
        if (cboProducto.SelectedValue is not int idProducto)
        {
            MostrarMensaje("Seleccione un producto.");
            return;
        }

        if (!decimal.TryParse(txtCosto.Text, out var costo))
        {
            MostrarMensaje("Ingrese un costo unitario válido.");
            return;
        }

        var cantidad = (int)nudCantidad.Value;

        var resultado = _compraService.AgregarProducto(idProducto, cantidad, costo);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            nudCantidad.Value = 1;
            txtCosto.Clear();
            ActualizarDetalle();
        }
    }

    private void btnEliminarLinea_Click(object? sender, EventArgs e)
    {
        if (dgvDetalle.CurrentRow?.DataBoundItem is not DetalleCompra detalle)
        {
            MostrarMensaje("Seleccione un producto del detalle.");
            return;
        }

        _compraService.EliminarProducto(detalle.IdProducto);
        ActualizarDetalle();
    }

    private void btnConfirmar_Click(object? sender, EventArgs e)
    {
        if (cboProveedor.SelectedValue is not int idProveedor)
        {
            MostrarMensaje("Seleccione un proveedor.");
            return;
        }

        var resultado = _compraService.ConfirmarCompra(idProveedor);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            ActualizarDetalle();
            CargarProductos(); // Refrescar para mostrar existencias actualizadas
        }
    }

    private void btnCancelar_Click(object? sender, EventArgs e)
    {
        _compraService.IniciarCompra();
        ActualizarDetalle();
        MostrarMensaje("Compra cancelada.", esError: false);
    }

    private void ActualizarDetalle()
    {
        dgvDetalle.DataSource = null;
        dgvDetalle.DataSource = _compraService.Detalle.ToList();

        if (dgvDetalle.Columns.Count > 0 && dgvDetalle.Columns["IdDetalleCompra"] != null)
        {
            dgvDetalle.Columns["IdDetalleCompra"].Visible = false;
            dgvDetalle.Columns["IdCompra"].Visible = false;
            dgvDetalle.Columns["IdProducto"].Visible = false;
            dgvDetalle.Columns["NombreProducto"].HeaderText = "Producto";
            dgvDetalle.Columns["Cantidad"].HeaderText = "Cantidad";
            dgvDetalle.Columns["CostoUnitario"].HeaderText = "Costo Unit.";
            dgvDetalle.Columns["Subtotal"].HeaderText = "Subtotal";
        }

        lblTotal.Text = $"Total: {_compraService.Total:C2}";
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
