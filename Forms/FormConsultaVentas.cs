using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Consulta histórica de ventas con filtros por fecha y número de venta.
/// Al seleccionar una venta muestra su detalle completo (sección 15 lineamientos).
/// </summary>
public partial class FormConsultaVentas : Form
{
    private readonly VentaRepository _ventaRepository = new();

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
            dgvVentas.Columns["IdVenta"].HeaderText = "# Venta";
            dgvVentas.Columns["FechaHora"].HeaderText = "Fecha / Hora";
            dgvVentas.Columns["Total"].HeaderText = "Total";
            dgvVentas.Columns["MetodoPago"].HeaderText = "Método de pago";
            dgvVentas.Columns["MontoRecibido"].HeaderText = "Recibido";
            dgvVentas.Columns["Cambio"].HeaderText = "Cambio";
            dgvVentas.Columns["IdCaja"].HeaderText = "# Caja";
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
            var venta = _ventaRepository.ObtenerPorId((int)nudIdVenta.Value);
            return venta is null ? new List<Venta>() : new List<Venta> { venta };
        }

        return _ventaRepository.ObtenerPorRango(dtpDesde.Value.Date, dtpHasta.Value.Date);
    }

    private void dgvVentas_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvVentas.CurrentRow?.DataBoundItem is not Venta venta) return;

        var ventaCompleta = _ventaRepository.ObtenerPorId(venta.IdVenta);
        if (ventaCompleta is null) return;

        dgvDetalle.DataSource = null;
        dgvDetalle.DataSource = ventaCompleta.Detalles;

        if (dgvDetalle.Columns.Count > 0 && dgvDetalle.Columns["IdDetalleVenta"] != null)
        {
            dgvDetalle.Columns["IdDetalleVenta"].Visible = false;
            dgvDetalle.Columns["IdVenta"].Visible = false;
            dgvDetalle.Columns["IdProducto"].Visible = false;
            dgvDetalle.Columns["NombreProducto"].HeaderText = "Producto";
            dgvDetalle.Columns["Cantidad"].HeaderText = "Cantidad";
            dgvDetalle.Columns["PrecioUnitario"].HeaderText = "Precio Unit.";
            dgvDetalle.Columns["Subtotal"].HeaderText = "Subtotal";
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

        var ventaCompleta = _ventaRepository.ObtenerPorId(venta.IdVenta);
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
