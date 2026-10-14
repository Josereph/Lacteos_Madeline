using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de registro de compras: seleccionar proveedor, agregar
/// productos con cantidad y costo, confirmar y actualizar inventario
/// automáticamente (sección 5 y 7 de los lineamientos).
/// </summary>
public partial class FormCompras : Form
{
    private readonly CompraService _compraService = new();

    public FormCompras()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
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
        var proveedores = _compraService.ProveedoresActivos();
        cboProveedor.DisplayMember = "Nombre";
        cboProveedor.ValueMember = "IdProveedor";
        cboProveedor.DataSource = proveedores;
    }

    private void CargarProductos()
    {
        var productos = _compraService.ProductosActivos();
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

        if (!Dinero.TryParse(txtCosto.Text, out var costo))
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
            dgvDetalle.OcultarColumna("IdDetalleCompra");
            dgvDetalle.OcultarColumna("IdCompra");
            dgvDetalle.OcultarColumna("IdProducto");
            dgvDetalle.Encabezado("NombreProducto", "Producto");
            dgvDetalle.Encabezado("Cantidad", "Cantidad");
            dgvDetalle.Encabezado("CostoUnitario", "Costo Unit.");
            dgvDetalle.Encabezado("Subtotal", "Subtotal");
        }

        lblTotal.Text = $"Total: {_compraService.Total:C2}";
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
        Diseno.Anclar(Diseno.Todo, dgvDetalle);
        Diseno.Anclar(Diseno.AbajoIzquierda, btnEliminarLinea);
        Diseno.Anclar(Diseno.AbajoDerecha, lblTotal, btnConfirmar, btnCancelar);
        Diseno.Anclar(Diseno.AbajoAncho, lblMensaje);
    }
}
