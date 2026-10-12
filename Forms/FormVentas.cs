using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla principal de ventas: seleccionar producto, indicar cantidad,
/// calcular subtotales y total, elegir método de pago, cobrar (con cálculo
/// de cambio en efectivo, RN-12), confirmar la venta (lo cual descuenta el
/// inventario automáticamente) e imprimir el ticket. Sigue el flujo de la
/// sección 8 de los lineamientos.
/// Atajos: Enter en cantidad = agregar, F12 = confirmar, Esc = cancelar venta.
/// </summary>
public partial class FormVentas : Form
{
    private readonly VentaService _ventaService = new();
    private readonly ProductoService _productoService = new();

    public FormVentas()
    {
        InitializeComponent();
        Load += FormVentas_Load;
        KeyDown += FormVentas_KeyDown;
        nudCantidad.KeyDown += nudCantidad_KeyDown;
    }

    private void FormVentas_Load(object? sender, EventArgs e)
    {
        cboMetodoPago.DataSource = Venta.MetodosPago.ToList();
        cboMetodoPago.SelectedItem = Venta.PagoEfectivo;

        _ventaService.IniciarVenta();
        CargarProductos();
        ActualizarCarrito();

        if (!_ventaService.HayCajaAbierta())
        {
            MostrarMensaje("Atención: no hay una caja abierta. Abra la caja en el módulo Caja para poder confirmar ventas.");
        }

        cboProducto.Focus();
    }

    private void FormVentas_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F12)
        {
            e.Handled = true;
            btnConfirmar_Click(sender, e);
        }
        else if (e.KeyCode == Keys.Escape && _ventaService.Carrito.Count > 0)
        {
            e.Handled = true;
            btnCancelar_Click(sender, e);
        }
    }

    private void nudCantidad_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            btnAgregar_Click(sender, e);
        }
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
            MostrarMensaje("Seleccione un producto de la lista.");
            return;
        }

        var cantidad = (int)nudCantidad.Value;

        var resultado = _ventaService.AgregarProducto(idProducto, cantidad);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            nudCantidad.Value = 1;
            lblCambio.Text = string.Empty;
            ActualizarCarrito();
            cboProducto.Focus();
            cboProducto.SelectAll();
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
        if (_ventaService.Carrito.Count == 0)
        {
            MostrarMensaje("Debe agregar al menos un producto a la venta.");
            return;
        }

        var metodoPago = cboMetodoPago.SelectedItem as string ?? Venta.PagoEfectivo;
        decimal? montoRecibido = null;

        if (metodoPago == Venta.PagoEfectivo)
        {
            var total = _ventaService.Total;
            using var dlg = new FormInputMonto(
                "Cobro en efectivo",
                $"Total a pagar: {total:C2}\nMonto recibido del cliente:",
                total,
                totalAPagar: total);

            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                MostrarMensaje("Cobro cancelado. La venta sigue abierta.");
                return;
            }

            montoRecibido = dlg.Monto;
        }

        var resultado = _ventaService.ConfirmarVenta(metodoPago, montoRecibido);

        if (!resultado.Exitoso || resultado.VentaRegistrada is null)
        {
            MostrarMensaje(resultado.Mensaje);
            return;
        }

        var venta = resultado.VentaRegistrada;
        MostrarMensaje($"Venta #{venta.IdVenta} registrada correctamente.", esError: false);

        lblCambio.Text = venta.Cambio is decimal cambio
            ? $"Recibido: {venta.MontoRecibido:C2}   CAMBIO A ENTREGAR: {cambio:C2}"
            : $"Pagado con {venta.MetodoPago.ToLower()}: {venta.Total:C2}";

        var vistaPrevia = TicketHelper.GenerarTexto(venta);
        var confirmarImpresion = MessageBox.Show(
            $"{vistaPrevia}\n¿Desea imprimir el ticket?",
            "Venta registrada",
            MessageBoxButtons.YesNo);

        if (confirmarImpresion == DialogResult.Yes)
        {
            var impresion = TicketHelper.Imprimir(venta);
            if (!impresion.Exitoso)
            {
                MessageBox.Show(
                    $"La venta quedó registrada, pero no se pudo imprimir el ticket.\n\n{impresion.Mensaje}\n\n" +
                    "Puede reimprimirlo desde Consulta de Ventas.",
                    "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        CargarProductos();
        ActualizarCarrito();
        cboMetodoPago.SelectedItem = Venta.PagoEfectivo;
        cboProducto.Focus();
    }

    private void btnCancelar_Click(object? sender, EventArgs e)
    {
        _ventaService.IniciarVenta();
        ActualizarCarrito();
        lblCambio.Text = string.Empty;
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
            dgvCarrito.Columns["PrecioUnitario"].DefaultCellStyle.Format = "C2";
            dgvCarrito.Columns["Subtotal"].HeaderText = "Subtotal";
            dgvCarrito.Columns["Subtotal"].DefaultCellStyle.Format = "C2";
        }

        lblTotal.Text = $"Total: {_ventaService.Total:C2}";
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
