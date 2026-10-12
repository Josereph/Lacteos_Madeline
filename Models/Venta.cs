namespace LacteosMadeline.Models;

/// <summary>
/// Cabecera de una venta, junto con el detalle de productos vendidos.
/// Registra el método de pago y, para pagos en efectivo, el monto
/// recibido y el cambio entregado (regla RN-12). IdCaja vincula la venta
/// con la jornada de caja en la que se realizó.
/// </summary>
public class Venta
{
    public const string PagoEfectivo = "Efectivo";
    public const string PagoTarjeta = "Tarjeta";
    public const string PagoTransferencia = "Transferencia";

    public static readonly string[] MetodosPago = { PagoEfectivo, PagoTarjeta, PagoTransferencia };

    public int IdVenta { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = PagoEfectivo;
    public decimal? MontoRecibido { get; set; }
    public decimal? Cambio { get; set; }
    public int? IdCaja { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = new();
}
