using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla principal de ventas: buscar el producto por nombre o código
/// (lector de código de barras), indicar la cantidad (con decimales si se
/// vende por peso), aplicar descuento, elegir cliente y método de pago,
/// cobrar (con cálculo de cambio en efectivo, RN-12), confirmar la venta
/// (lo cual descuenta el inventario automáticamente) e imprimir el ticket.
/// Sigue el flujo de la sección 8 de los lineamientos.
/// Atajos: Enter = agregar, F4 = descuento, F12 = confirmar, Esc = cancelar venta.
/// </summary>
public partial class FormVentas : Form
{
    private readonly VentaService _ventaService = new();
    private readonly ProductoService _productoService = new();
    private readonly ClienteService _clienteService = new();
    private bool _cargado;
    private bool _cargandoClientes;

    public FormVentas()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        lblTotal.ForeColor = Tema.CafeRustico;
        Load += FormVentas_Load;
        KeyDown += FormVentas_KeyDown;
        nudCantidad.KeyDown += nudCantidad_KeyDown;
        cboProducto.KeyDown += cboProducto_KeyDown;
        cboProducto.SelectedIndexChanged += (_, _) => Construir.ConfigurarCantidad(nudCantidad, ProductoSeleccionado);
        cboProducto.Format += (_, e) =>
        {
            if (e.ListItem is Producto p)
            {
                e.Value = p.PermiteDecimales ? $"{p.Nombre}  ({p.PrecioVenta:C2}/{UnidadMedida.Abreviatura(p.Unidad)})" : p.Nombre;
            }
        };
        cboCliente.SelectedIndexChanged += (_, _) => SeleccionarCliente();
        VisibleChanged += FormVentas_VisibleChanged;
    }

    /// <summary>
    /// Indica si hay productos en el carrito sin cobrar (el shell lo usa
    /// para advertir antes de cerrar el sistema).
    /// </summary>
    public bool TieneVentaEnCurso => _ventaService.Carrito.Count > 0;

    private Producto? ProductoSeleccionado => cboProducto.SelectedItem as Producto;

    private string MetodoSeleccionado =>
        rbTarjeta.Checked ? Venta.PagoTarjeta :
        rbTransferencia.Checked ? Venta.PagoTransferencia :
        Venta.PagoEfectivo;

    private void FormVentas_Load(object? sender, EventArgs e)
    {
        rbEfectivo.Checked = true;

        _ventaService.IniciarVenta();
        CargarProductos();
        CargarClientes();
        ActualizarCarrito();
        VerificarCaja();
        _cargado = true;

        cboProducto.Focus();
    }

    /// <summary>
    /// El punto de venta se conserva al cambiar de módulo (no se pierde el
    /// carrito). Al volver, se recargan existencias, clientes y estado de
    /// caja por si cambiaron en otros módulos.
    /// </summary>
    private void FormVentas_VisibleChanged(object? sender, EventArgs e)
    {
        if (!_cargado || !Visible) return;

        var seleccionado = cboProducto.SelectedValue;
        CargarProductos();
        if (seleccionado is not null) cboProducto.SelectedValue = seleccionado;
        CargarClientes(_ventaService.Cliente?.IdCliente);
        VerificarCaja();
        cboProducto.Focus();
    }

    private void VerificarCaja()
    {
        if (!_ventaService.HayCajaAbierta())
        {
            MostrarMensaje("Atención: no hay una caja abierta. Abra la caja en Control de Caja para poder cobrar.");
        }
        else if (lblMensaje.Text.StartsWith("Atención: no hay una caja abierta"))
        {
            MostrarMensaje(string.Empty, esError: false);
        }
    }

    private void FormVentas_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F12)
        {
            e.Handled = true;
            btnConfirmar_Click(sender, e);
        }
        else if (e.KeyCode == Keys.F4)
        {
            e.Handled = true;
            btnDescuento_Click(sender, e);
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

    /// <summary>
    /// Enter en el buscador: si el texto es un código de producto (lector
    /// de código de barras) se agrega directamente; si es un nombre, pasa
    /// a la cantidad.
    /// </summary>
    private void cboProducto_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        var texto = cboProducto.Text.Trim();
        if (texto.Length > 0 && _productoService.ObtenerPorCodigo(texto) is { } porCodigo)
        {
            var resultado = _ventaService.AgregarPorCodigo(texto, porCodigo.PermiteDecimales ? nudCantidad.Value : 1);
            MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                DespuesDeAgregar();
            }
            else
            {
                cboProducto.SelectAll();
            }

            return;
        }

        if (ProductoSeleccionado is not null)
        {
            nudCantidad.Focus();
            nudCantidad.Select(0, nudCantidad.Text.Length);
        }
        else
        {
            MostrarMensaje($"No se encontró ningún producto con \"{texto}\".");
        }
    }

    private void CargarProductos()
    {
        var productos = _productoService.Buscar(soloActivos: true);
        cboProducto.DisplayMember = "Nombre";
        cboProducto.ValueMember = "IdProducto";
        cboProducto.DataSource = productos;
        Construir.ConfigurarCantidad(nudCantidad, ProductoSeleccionado);
    }

    private void CargarClientes(int? idSeleccionado = null)
    {
        _cargandoClientes = true;
        var clientes = new List<Cliente> { new() { IdCliente = 0, Nombre = Cliente.ConsumidorFinal } };
        clientes.AddRange(_clienteService.Buscar(soloActivos: true));

        cboCliente.DisplayMember = "Nombre";
        cboCliente.ValueMember = "IdCliente";
        cboCliente.DataSource = clientes;
        cboCliente.SelectedValue = idSeleccionado ?? 0;
        _cargandoClientes = false;
        SeleccionarCliente();
    }

    private void SeleccionarCliente()
    {
        if (_cargandoClientes) return;

        var id = cboCliente.SelectedValue is int valor && valor > 0 ? valor : (int?)null;
        var resultado = _ventaService.SeleccionarCliente(id);
        if (!resultado.Exitoso) MostrarMensaje(resultado.Mensaje);
    }

    private void btnNuevoCliente_Click(object? sender, EventArgs e)
    {
        using var dialogo = new FormClienteRapido();
        if (dialogo.ShowDialog(this) == DialogResult.OK)
        {
            CargarClientes(dialogo.IdCliente);
        }
    }

    private void btnAgregar_Click(object? sender, EventArgs e)
    {
        if (ProductoSeleccionado is not { } producto)
        {
            MostrarMensaje("Seleccione un producto de la lista.");
            return;
        }

        var resultado = _ventaService.AgregarProducto(producto.IdProducto, nudCantidad.Value);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            DespuesDeAgregar();
        }
    }

    private void DespuesDeAgregar()
    {
        Construir.ConfigurarCantidad(nudCantidad, ProductoSeleccionado);
        LimpiarCambio();
        ActualizarCarrito();
        cboProducto.Focus();
        cboProducto.SelectAll();
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

    private void btnDescuento_Click(object? sender, EventArgs e)
    {
        if (_ventaService.Carrito.Count == 0)
        {
            MostrarMensaje("Agregue productos antes de aplicar un descuento.");
            return;
        }

        using var dialogo = new FormDescuento(_ventaService.Subtotal, _ventaService.Descuento);
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        int? idAutoriza = null;
        if (dialogo.Valor > 0)
        {
            var autorizacion = FormAutorizacion.Solicitar(this, "Aplicar un descuento");
            if (!autorizacion.Autorizado)
            {
                MostrarMensaje("Descuento no autorizado.");
                return;
            }

            idAutoriza = autorizacion.IdAutoriza;
        }

        var resultado = dialogo.EsPorcentaje
            ? _ventaService.AplicarDescuentoPorcentaje(dialogo.Valor, idAutoriza)
            : _ventaService.AplicarDescuento(dialogo.Valor, idAutoriza);

        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);
        ActualizarCarrito();
    }

    private void btnConfirmar_Click(object? sender, EventArgs e)
    {
        if (_ventaService.Carrito.Count == 0)
        {
            MostrarMensaje("Debe agregar al menos un producto a la venta.");
            return;
        }

        var metodoPago = MetodoSeleccionado;
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

        // Cambio en gran formato para entregarlo sin errores.
        lblCambio.Text = $"{venta.Cambio ?? 0:C2}";
        lblCambio.ForeColor = venta.Cambio > 0 ? Tema.Exito : Tema.Carbon;
        lblRecibido.Text = venta.MontoRecibido is decimal recibido
            ? $"Venta N.º {venta.IdVenta}  ·  Recibido {recibido:C2}"
            : $"Venta N.º {venta.IdVenta}  ·  Pagado con {venta.MetodoPago.ToLower()}";

        var vistaPrevia = TicketHelper.GenerarTexto(venta);
        var confirmarImpresion = MessageBox.Show(
            $"{vistaPrevia}\n¿Desea imprimir el ticket?",
            "Venta registrada",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmarImpresion == DialogResult.Yes)
        {
            var impresion = TicketHelper.Imprimir(venta);
            if (!impresion.Exitoso)
            {
                MessageBox.Show(
                    $"La venta quedó registrada, pero no se pudo imprimir el ticket.\n\n{impresion.Mensaje}\n\n" +
                    "Puede reimprimirlo desde Ventas Realizadas.",
                    "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        CargarProductos();
        CargarClientes();
        ActualizarCarrito();
        rbEfectivo.Checked = true;
        cboProducto.Focus();
    }

    private void btnCancelar_Click(object? sender, EventArgs e)
    {
        _ventaService.IniciarVenta();
        CargarClientes();
        ActualizarCarrito();
        LimpiarCambio();
        MostrarMensaje("Venta cancelada.", esError: false);
    }

    private void ActualizarCarrito()
    {
        dgvCarrito.DataSource = null;
        dgvCarrito.DataSource = _ventaService.Carrito.ToList();

        dgvCarrito.OcultarColumna("IdProducto");
        dgvCarrito.OcultarColumna("IdDetalleVenta");
        dgvCarrito.OcultarColumna("IdVenta");
        dgvCarrito.Encabezado("NombreProducto", "Producto");
        dgvCarrito.Encabezado("Cantidad", "Cantidad");
        dgvCarrito.FormatoColumna("Cantidad", "0.###");
        dgvCarrito.Encabezado("Unidad", "Unidad");
        dgvCarrito.Encabezado("PrecioUnitario", "Precio Unitario");
        dgvCarrito.FormatoColumna("PrecioUnitario", "C2");
        dgvCarrito.Encabezado("Subtotal", "Subtotal");
        dgvCarrito.FormatoColumna("Subtotal", "C2");

        lblTotal.Text = $"{_ventaService.Total:C2}";
        lblDescuento.Text = _ventaService.Descuento > 0
            ? $"Subtotal {_ventaService.Subtotal:C2}  ·  Descuento -{_ventaService.Descuento:C2}"
            : $"Subtotal {_ventaService.Subtotal:C2}";
        lblDescuento.ForeColor = _ventaService.Descuento > 0 ? Tema.Exito : Tema.GrisCalido;

        var lineas = _ventaService.Carrito.Count;
        lblDesglose.Text = lineas == 0
            ? "Sin productos"
            : $"{lineas} {(lineas == 1 ? "producto" : "productos")}";
    }

    private void LimpiarCambio()
    {
        lblCambio.Text = $"{0m:C2}";
        lblCambio.ForeColor = Tema.GrisCalido;
        lblRecibido.Text = string.Empty;
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        lblMensaje.Text = mensaje;
    }
}
