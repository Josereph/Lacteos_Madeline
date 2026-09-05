using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

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
            dgvVentas.Columns["Detalles"].Visible = false;
        }

        dgvDetalle.DataSource = null;
        lblTotalVentas.Text = $"Ventas encontradas: {ventas.Count}  |  Total: {ventas.Sum(v => v.Total):C2}";
    }

    private List<Venta> ObtenerVentasFiltradas()
    {
        var todas = _ventaRepository.ObtenerHistorial();

        // Filtro por número
        if (nudIdVenta.Value > 0)
        {
            var idBuscado = (int)nudIdVenta.Value;
            todas = todas.Where(v => v.IdVenta == idBuscado).ToList();
        }
        else
        {
            // Filtro por rango de fechas
            var desde = dtpDesde.Value.Date;
            var hasta = dtpHasta.Value.Date.AddDays(1).AddSeconds(-1);
            todas = todas.Where(v => v.FechaHora >= desde && v.FechaHora <= hasta).ToList();
        }

        return todas;
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
            TicketHelper.Imprimir(ventaCompleta);
        }
    }
}
