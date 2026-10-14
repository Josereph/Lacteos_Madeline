using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Consulta histórica de ventas con filtros por fecha y número de venta.
/// Al seleccionar una venta muestra su detalle completo (sección 15 lineamientos).
/// </summary>
public partial class FormConsultaVentas : Form
{
    private readonly VentaService _ventaService = new();

    public FormConsultaVentas()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormConsultaVentas_Load;
    }

    private void FormConsultaVentas_Load(object? sender, EventArgs e)
    {
        dtpDesde.Value = DateTime.Today.AddDays(-30);
        dtpHasta.Value = DateTime.Today;
        BuscarVentas();
    }

    private void btnBuscar_Click(object? sender, EventArgs e)
    {
        BuscarVentas();
    }

    private void BuscarVentas()
    {
        var ventas = ObtenerVentasFiltradas();

        dgvVentas.DataSource = null;
        dgvVentas.DataSource = ventas;

        if (dgvVentas.Columns.Count > 0 && dgvVentas.Columns["IdVenta"] != null)
        {
            dgvVentas.Encabezado("IdVenta", "# Venta");
            dgvVentas.Encabezado("FechaHora", "Fecha / Hora");
            dgvVentas.Encabezado("Total", "Total");
            dgvVentas.Encabezado("MetodoPago", "Método de pago");
            dgvVentas.Encabezado("MontoRecibido", "Recibido");
            dgvVentas.Encabezado("Cambio", "Cambio");
            dgvVentas.Encabezado("IdCaja", "# Caja");
            if (dgvVentas.Columns["Detalles"] is { } colDetalles) colDetalles.Visible = false;
        }

        dgvDetalle.DataSource = null;
        lblTotalVentas.Text = $"Ventas encontradas: {ventas.Count}  |  Total: {ventas.Sum(v => v.Total):C2}";
    }

    private List<Venta> ObtenerVentasFiltradas()
    {
        // El filtro se resuelve en SQL (por clave o por índice de fecha) en
        // lugar de cargar todo el historial en memoria y filtrar aquí.
        if (nudIdVenta.Value > 0)
        {
            var venta = _ventaService.ObtenerPorId((int)nudIdVenta.Value);
            return venta is null ? new List<Venta>() : new List<Venta> { venta };
        }

        return _ventaService.ConsultarPorRango(dtpDesde.Value.Date, dtpHasta.Value.Date);
    }

    private void dgvVentas_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvVentas.CurrentRow?.DataBoundItem is not Venta venta) return;

        var ventaCompleta = _ventaService.ObtenerPorId(venta.IdVenta);
        if (ventaCompleta is null) return;

        dgvDetalle.DataSource = null;
        dgvDetalle.DataSource = ventaCompleta.Detalles;

        if (dgvDetalle.Columns.Count > 0 && dgvDetalle.Columns["IdDetalleVenta"] != null)
        {
            dgvDetalle.OcultarColumna("IdDetalleVenta");
            dgvDetalle.OcultarColumna("IdVenta");
            dgvDetalle.OcultarColumna("IdProducto");
            dgvDetalle.Encabezado("NombreProducto", "Producto");
            dgvDetalle.Encabezado("Cantidad", "Cantidad");
            dgvDetalle.Encabezado("PrecioUnitario", "Precio Unit.");
            dgvDetalle.Encabezado("Subtotal", "Subtotal");
        }
    }

    private void btnReimprimir_Click(object? sender, EventArgs e)
    {
        if (dgvVentas.CurrentRow?.DataBoundItem is not Venta venta)
        {
            MessageBox.Show("Seleccione una venta de la lista.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ventaCompleta = _ventaService.ObtenerPorId(venta.IdVenta);
        if (ventaCompleta is null) return;

        var texto = TicketHelper.GenerarTexto(ventaCompleta);
        var respuesta = MessageBox.Show(
            texto + "\n¿Desea imprimir este ticket?",
            $"Ticket – Venta #{ventaCompleta.IdVenta}",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (respuesta == DialogResult.Yes)
        {
            var impresion = TicketHelper.Imprimir(ventaCompleta);
            if (!impresion.Exitoso)
            {
                MessageBox.Show($"No se pudo imprimir el ticket.\n\n{impresion.Mensaje}",
                    "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        Diseno.Anclar(Diseno.ArribaAncho, dgvVentas);
        Diseno.Anclar(Diseno.ArribaDerecha, btnReimprimir);
        Diseno.Anclar(Diseno.Todo, dgvDetalle);
    }
}
