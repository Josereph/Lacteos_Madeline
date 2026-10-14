using System.ComponentModel;

namespace LacteosMadeline.Models;

/// <summary>
/// Cabecera de una venta, junto con el detalle de productos vendidos.
/// Registra el método de pago y, para pagos en efectivo, el monto
/// recibido y el cambio entregado (regla RN-12). IdCaja vincula la venta
/// con la jornada de caja en la que se realizó.
/// <see cref="Total"/> es lo cobrado: la suma de las líneas menos el
/// <see cref="Descuento"/>. Una venta no se modifica ni se elimina: se anula.
/// </summary>
public class Venta
{
    public const string PagoEfectivo = "Efectivo";
    public const string PagoTarjeta = "Tarjeta";
    public const string PagoTransferencia = "Transferencia";

    public const string EstadoActiva = "Activa";
    public const string EstadoAnulada = "Anulada";

    public static readonly string[] MetodosPago = { PagoEfectivo, PagoTarjeta, PagoTransferencia };

    public int IdVenta { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public string Estado { get; set; } = EstadoActiva;
    public string? NombreCliente { get; set; }
    public string MetodoPago { get; set; } = PagoEfectivo;
    public decimal Total { get; set; }
    public decimal Descuento { get; set; }
    public decimal? MontoRecibido { get; set; }
    public decimal? Cambio { get; set; }
    public string? NombreUsuario { get; set; }
    public int? IdCaja { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? NombreUsuarioAnulacion { get; set; }

    [Browsable(false)] public int? IdCliente { get; set; }
    [Browsable(false)] public int? IdUsuario { get; set; }
    [Browsable(false)] public int? IdUsuarioDescuento { get; set; }
    [Browsable(false)] public int? IdCajaAnulacion { get; set; }
    [Browsable(false)] public List<DetalleVenta> Detalles { get; set; } = new();

    /// <summary>Suma de las líneas antes del descuento.</summary>
    [Browsable(false)] public decimal Subtotal => Total + Descuento;

    [Browsable(false)] public bool EsAnulada => Estado == EstadoAnulada;
}
