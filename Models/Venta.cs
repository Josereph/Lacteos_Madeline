namespace LacteosMadeline.Models;

/// <summary>
/// Cabecera de una venta, junto con el detalle de productos vendidos.
/// </summary>
public class Venta
{
    public int IdVenta { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = new();
}
