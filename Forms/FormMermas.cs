using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Registro de bajas de inventario por caducidad, daño u otro motivo.
/// Toda la lógica (validaciones, transacción, descuento de stock) vive en
/// MermaService; este formulario solo captura datos y muestra resultados.
/// Captura a la izquierda, historial de auditoría a la derecha.
/// </summary>
public partial class FormMermas : Form
{
    private readonly MermaService _mermaService = new();
    private readonly ProductoService _productoService = new();

    private List<Producto> _productos = new();

    public FormMermas()
    {
        InitializeComponent();
        Tema.Aplicar(this);

        Load += FormMermas_Load;
        cboProducto.SelectedIndexChanged += (_, _) => ActualizarProductoSeleccionado();
        cboProducto.Format += cboProducto_Format;
        cboMotivo.SelectedIndexChanged += (_, _) => ActualizarMotivo();
        nudCantidad.ValueChanged += (_, _) => ActualizarCostoEstimado();
        txtObservacion.TextChanged += (_, _) => ActualizarMotivo();
    }

    private void FormMermas_Load(object? sender, EventArgs e)
    {
        cboMotivo.DataSource = Merma.Motivos.ToList();
        dtpDesde.Value = DateTime.Today.AddDays(-30);
        dtpHasta.Value = DateTime.Today;

        CargarProductos();
        CargarHistorial();
        ActualizarMotivo();
        cboProducto.Focus();
    }

    // ── Captura ─────────────────────────────────────────────────────────

    private void CargarProductos()
    {
        // Solo productos con existencia: no se puede dar de baja lo que no hay.
        _productos = _productoService.Buscar()
            .Where(p => p.Existencia > 0)
            .ToList();

        cboProducto.DisplayMember = nameof(Producto.Nombre);
        cboProducto.ValueMember = nameof(Producto.IdProducto);
        cboProducto.DataSource = _productos;
        cboProducto.SelectedIndex = _productos.Count > 0 ? 0 : -1;
        ActualizarProductoSeleccionado();
    }

    /// <summary>
    /// Muestra el stock junto al nombre en la lista desplegable
    /// (p. ej. "Queso fresco  ·  stock 25").
    /// </summary>
    private void cboProducto_Format(object? sender, ListControlConvertEventArgs e)
    {
        if (e.ListItem is Producto p)
        {
            e.Value = $"{p.Nombre}  ·  stock {Cantidades.Formatear(p.Existencia, p.Unidad)}";
        }
    }

    private Producto? ProductoSeleccionado => cboProducto.SelectedItem as Producto;

    private void ActualizarProductoSeleccionado()
    {
        var producto = ProductoSeleccionado;

        if (producto is null)
        {
            lblExistencia.Text = _productos.Count == 0
                ? "No hay productos con existencia para dar de baja."
                : "Seleccione un producto de la lista.";
            nudCantidad.Maximum = 1;
            nudCantidad.Enabled = false;
        }
        else
        {
            lblExistencia.Text = $"Existencia actual: {Cantidades.Formatear(producto.Existencia, producto.Unidad)}  " +
                                 $"(mínimo {Cantidades.Formatear(producto.StockMinimo, producto.Unidad)})";
            // La cantidad no puede superar la existencia (el servicio lo vuelve a validar).
            nudCantidad.Enabled = true;
            Construir.ConfigurarCantidad(nudCantidad, producto, producto.Existencia);
        }

        ActualizarCostoEstimado();
    }

    private void ActualizarCostoEstimado()
    {
        var producto = ProductoSeleccionado;
        lblCostoEstimado.Text = producto?.PrecioCompra is decimal costo
            ? $"Pérdida estimada: {costo * nudCantidad.Value:C2}"
            : producto is null ? string.Empty : "Pérdida estimada: sin precio de compra registrado";
    }

    /// <summary>
    /// Si el motivo es "Otro", la observación pasa a ser obligatoria y se
    /// resalta en terracota hasta que se escriba algo.
    /// </summary>
    private void ActualizarMotivo()
    {
        var esOtro = cboMotivo.SelectedItem as string == Merma.MotivoOtro;
        var falta = esOtro && string.IsNullOrWhiteSpace(txtObservacion.Text);

        lblObservacion.Text = esOtro ? "Observación (obligatoria):" : "Observación (opcional):";
        lblObservacion.ForeColor = esOtro ? Tema.Alerta : Tema.Carbon;
        lblObservacion.Font = new Font(Tema.Familia, 9.75F, esOtro ? FontStyle.Bold : FontStyle.Regular);
        txtObservacion.BackColor = falta ? Tema.TerracotaTenue : Color.White;
        txtObservacion.PlaceholderText = esOtro ? "Describa el motivo de la baja…" : string.Empty;
    }

    private void btnRegistrarMerma_Click(object? sender, EventArgs e)
    {
        var producto = ProductoSeleccionado;
        if (producto is null)
        {
            MostrarMensaje("Seleccione un producto de la lista.");
            cboProducto.Focus();
            return;
        }

        var motivo = cboMotivo.SelectedItem as string ?? string.Empty;
        var cantidad = nudCantidad.Value;

        if (motivo == Merma.MotivoOtro && string.IsNullOrWhiteSpace(txtObservacion.Text))
        {
            MostrarMensaje("Con el motivo \"Otro\" la observación es obligatoria.");
            txtObservacion.Focus();
            return;
        }

        // La baja descuenta inventario y no se puede deshacer desde el sistema.
        var confirmar = MessageBox.Show(
            $"¿Dar de baja {Cantidades.Formatear(cantidad, producto.Unidad)} × {producto.Nombre}?\n\nMotivo: {motivo}\n" +
            "Esta acción descuenta el inventario.",
            "Confirmar baja de inventario",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirmar != DialogResult.Yes) return;

        var resultado = _mermaService.Registrar(producto.IdProducto, cantidad, motivo, txtObservacion.Text);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            var idProducto = producto.IdProducto;
            LimpiarCaptura();
            CargarProductos();
            SeleccionarProducto(idProducto);
            CargarHistorial();
        }
    }

    private void btnLimpiar_Click(object? sender, EventArgs e)
    {
        LimpiarCaptura();
        MostrarMensaje(string.Empty, esError: false);
    }

    private void LimpiarCaptura()
    {
        Construir.ConfigurarCantidad(nudCantidad, ProductoSeleccionado, ProductoSeleccionado?.Existencia ?? 1);
        cboMotivo.SelectedItem = Merma.MotivoCaducidad;
        txtObservacion.Clear();
        cboProducto.Focus();
    }

    private void SeleccionarProducto(int idProducto)
    {
        var indice = _productos.FindIndex(p => p.IdProducto == idProducto);
        if (indice >= 0) cboProducto.SelectedIndex = indice;
    }

    // ── Historial ───────────────────────────────────────────────────────

    private void btnFiltrar_Click(object? sender, EventArgs e)
    {
        CargarHistorial();
    }

    private void CargarHistorial()
    {
        if (dtpDesde.Value.Date > dtpHasta.Value.Date)
        {
            MostrarMensaje("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.");
            return;
        }

        var mermas = _mermaService.Consultar(dtpDesde.Value.Date, dtpHasta.Value.Date);

        dgvMermas.DataSource = null;
        dgvMermas.DataSource = mermas;

        if (dgvMermas.Columns["IdMerma"] is not null)
        {
            dgvMermas.OcultarColumna("IdMerma");
            dgvMermas.OcultarColumna("IdProducto");
            dgvMermas.Encabezado("FechaHora", "Fecha");
            dgvMermas.Encabezado("NombreProducto", "Producto");
            dgvMermas.Encabezado("Cantidad", "Cant.");
            dgvMermas.FormatoColumna("Cantidad", "0.###");
            dgvMermas.Encabezado("Motivo", "Motivo");
            dgvMermas.OcultarColumna("CostoUnitario"); // la pérdida ya lo refleja
            dgvMermas.Encabezado("CostoTotal", "Pérdida");
            dgvMermas.Encabezado("Observacion", "Observación");

            dgvMermas.FormatoColumna("FechaHora", "dd/MM/yy HH:mm");
            dgvMermas.Columns["Cantidad"]!.FillWeight = 55;
            dgvMermas.Columns["Motivo"]!.FillWeight = 115;
        }

        var perdida = mermas.Sum(m => m.CostoTotal ?? 0);
        lblTotalPerdida.Text = mermas.Count == 0
            ? "Sin bajas en el período."
            : $"{mermas.Count} baja(s)  ·  Pérdida estimada: {perdida:C2}";
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        lblMensaje.Text = mensaje;
    }
}
