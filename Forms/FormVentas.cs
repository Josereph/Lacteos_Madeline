using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla principal de ventas: seleccionar producto, indicar cantidad,
/// calcular subtotales y total, confirmar la venta (lo cual descuenta el
/// inventario automáticamente) e imprimir el ticket. Sigue el flujo de la
/// sección 8 de los lineamientos.
/// </summary>
public partial class FormVentas : Form
{
    private readonly VentaService _ventaService = new();
    private readonly ProductoService _productoService = new();

    public FormVentas()
    {
        InitializeComponent();
        Load += FormVentas_Load;
    }

    private void FormVentas_Load(object? sender, EventArgs e)
    {
        _ventaService.IniciarVenta();
        CargarProductos();
        ActualizarCarrito();
    }

    private void CargarProductos()
    {
        var productos = _productoService.Buscar(soloActivos: true);
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

        var cantidad = (int)nudCantidad.Value;

        var resultado = _ventaService.AgregarProducto(idProducto, cantidad);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            nudCantidad.Value = 1;
            ActualizarCarrito();
        }
    }

    private void btnEliminarLinea_Click(object? sender, EventArgs e)
    {
        if (dgvCarrito.CurrentRow?.DataBoundItem is not DetalleVenta detalle)
        {
            MostrarMensaje("Seleccione un producto del detalle de la venta.");
            return;
        }

        _ventaService.EliminarProducto(detalle.IdProducto);
        ActualizarCarrito();
    }

    private void btnConfirmar_Click(object? sender, EventArgs e)
    {
        var resultado = _ventaService.ConfirmarVenta();

        if (!resultado.Exitoso)
        {
            MostrarMensaje(resultado.Mensaje);
            return;
        }

        MostrarMensaje(resultado.Mensaje, esError: false);

        if (resultado.VentaRegistrada is not null)
        {
            var vistaPrevia = TicketHelper.GenerarTexto(resultado.VentaRegistrada);
            var confirmarImpresion = MessageBox.Show(
                $"{vistaPrevia}\n¿Desea imprimir el ticket?",
                "Venta registrada",
                MessageBoxButtons.YesNo);

            if (confirmarImpresion == DialogResult.Yes)
            {
                TicketHelper.Imprimir(resultado.VentaRegistrada);
            }
        }

        CargarProductos();
        ActualizarCarrito();
    }

    private void btnCancelar_Click(object? sender, EventArgs e)
    {
        _ventaService.IniciarVenta();
        ActualizarCarrito();
        MostrarMensaje("Venta cancelada.", esError: false);
    }

    private void ActualizarCarrito()
    {
        dgvCarrito.DataSource = null;
        dgvCarrito.DataSource = _ventaService.Carrito.ToList();

        if (dgvCarrito.Columns["IdProducto"] != null)
        {
            dgvCarrito.Columns["IdProducto"].Visible = false;
            dgvCarrito.Columns["IdDetalleVenta"].Visible = false;
            dgvCarrito.Columns["IdVenta"].Visible = false;
            dgvCarrito.Columns["NombreProducto"].HeaderText = "Producto";
            dgvCarrito.Columns["Cantidad"].HeaderText = "Cantidad";
            dgvCarrito.Columns["PrecioUnitario"].HeaderText = "Precio Unitario";
            dgvCarrito.Columns["Subtotal"].HeaderText = "Subtotal";
        }

        lblTotal.Text = $"Total: {_ventaService.Total:C2}";
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
