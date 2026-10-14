using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Ventas realizadas: consulta con filtros por fecha y número de venta,
/// detalle de la venta seleccionada, reimpresión del ticket y anulación
/// (un cajero necesita la autorización de un administrador).
/// Las ventas anuladas se muestran en gris y no suman en el total.
/// </summary>
public partial class FormConsultaVentas : Form
{
    private readonly VentaService _ventaService = new();
    private Font? _fuenteAnulada;

    public FormConsultaVentas()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormConsultaVentas_Load;
        dgvVentas.CellFormatting += dgvVentas_CellFormatting;
        Disposed += (_, _) => _fuenteAnulada?.Dispose();
    }

    private void FormConsultaVentas_Load(object? sender, EventArgs e)
    {
        dtpDesde.Value = DateTime.Today;
        dtpHasta.Value = DateTime.Today;
        BuscarVentas();
    }

    private void btnBuscar_Click(object? sender, EventArgs e)
    {
        BuscarVentas();
    }

    private void BuscarVentas(int? idSeleccionar = null)
    {
        var ventas = ObtenerVentasFiltradas();

        dgvVentas.DataSource = null;
        dgvVentas.DataSource = ventas;

        dgvVentas.Encabezado("IdVenta", "# Venta");
        dgvVentas.Encabezado("FechaHora", "Fecha / Hora");
        dgvVentas.Encabezado("NombreCliente", "Cliente");
        dgvVentas.Encabezado("MetodoPago", "Método de pago");
        dgvVentas.FormatoColumna("Total", "C2");
        dgvVentas.FormatoColumna("Descuento", "C2");
        dgvVentas.Encabezado("MontoRecibido", "Recibido");
        dgvVentas.FormatoColumna("MontoRecibido", "C2");
        dgvVentas.FormatoColumna("Cambio", "C2");
        dgvVentas.Encabezado("NombreUsuario", "Atendió");
        dgvVentas.Encabezado("IdCaja", "# Caja");
        dgvVentas.Encabezado("FechaAnulacion", "Anulada el");
        dgvVentas.Encabezado("MotivoAnulacion", "Motivo de anulación");
        dgvVentas.Encabezado("NombreUsuarioAnulacion", "Anuló");

        var activas = ventas.Where(v => !v.EsAnulada).ToList();
        var anuladas = ventas.Count - activas.Count;
        lblTotalVentas.Text = $"Ventas: {activas.Count}  |  Total: {activas.Sum(v => v.Total):C2}" +
                              (anuladas > 0 ? $"  |  Anuladas: {anuladas}" : string.Empty);

        dgvDetalle.DataSource = null;

        if (idSeleccionar is int id)
        {
            foreach (DataGridViewRow fila in dgvVentas.Rows)
            {
                if (fila.DataBoundItem is Venta v && v.IdVenta == id)
                {
                    dgvVentas.CurrentCell = fila.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Sin cliente se muestra "Consumidor final". Las ventas anuladas se ven
    /// en gris y tachadas.
    /// </summary>
    private void dgvVentas_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || dgvVentas.Rows[e.RowIndex].DataBoundItem is not Venta venta) return;

        if (e.Value is null && dgvVentas.Columns[e.ColumnIndex].DataPropertyName == nameof(Venta.NombreCliente))
        {
            e.Value = Cliente.ConsumidorFinal;
            e.FormattingApplied = true;
        }

        if (!venta.EsAnulada) return;

        e.CellStyle!.ForeColor = Tema.GrisCalido;
        e.CellStyle.Font = _fuenteAnulada ??= new Font(dgvVentas.Font, FontStyle.Strikeout);
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

        dgvDetalle.OcultarColumna("IdDetalleVenta");
        dgvDetalle.OcultarColumna("IdVenta");
        dgvDetalle.OcultarColumna("IdProducto");
        dgvDetalle.Encabezado("NombreProducto", "Producto");
        dgvDetalle.FormatoColumna("Cantidad", "0.###");
        dgvDetalle.Encabezado("PrecioUnitario", "Precio Unit.");
        dgvDetalle.FormatoColumna("PrecioUnitario", "C2");
        dgvDetalle.FormatoColumna("Subtotal", "C2");
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
    /// Anula la venta seleccionada: pide el motivo y, si el usuario es
    /// cajero, la autorización de un administrador.
    /// </summary>
    private void btnAnularVenta_Click(object? sender, EventArgs e)
    {
        if (dgvVentas.CurrentRow?.DataBoundItem is not Venta venta)
        {
            MessageBox.Show(this, "Seleccione la venta que desea anular.", "Anular venta",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (venta.EsAnulada)
        {
            MessageBox.Show(this, $"La venta #{venta.IdVenta} ya está anulada.", "Anular venta",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var devolucion = venta.MetodoPago == Venta.PagoEfectivo
            ? "\n\nSi la venta es de una jornada anterior, el efectivo devuelto se registrará como salida de la caja actual."
            : $"\n\nLa venta se pagó con {venta.MetodoPago.ToLower()}: la devolución del dinero se gestiona por ese medio.";

        var confirmar = MessageBox.Show(this,
            $"¿Anular la venta #{venta.IdVenta} por {venta.Total:C2}?\n\n" +
            $"Los productos vuelven al inventario y la venta deja de contar en los totales.{devolucion}",
            "Anular venta", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirmar != DialogResult.Yes) return;

        using var dialogoMotivo = new FormEntradaTexto("Motivo de la anulación",
            "Escriba por qué se anula la venta (queda registrado).");
        if (dialogoMotivo.ShowDialog(this) != DialogResult.OK) return;

        var autorizacion = FormAutorizacion.Solicitar(this, "Anular una venta");
        if (!autorizacion.Autorizado) return;

        var resultado = _ventaService.Anular(venta.IdVenta, dialogoMotivo.Texto, autorizacion.IdAutoriza);
        MessageBox.Show(this, resultado.Mensaje, "Anular venta", MessageBoxButtons.OK,
            resultado.Exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (resultado.Exitoso)
        {
            BuscarVentas(venta.IdVenta);
        }
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        Diseno.Anclar(Diseno.ArribaAncho, dgvVentas);
        Diseno.Anclar(Diseno.ArribaDerecha, btnReimprimir, btnAnularVenta);
        Diseno.Anclar(Diseno.Todo, dgvDetalle);
    }
}
