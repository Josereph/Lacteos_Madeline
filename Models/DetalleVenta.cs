using System.ComponentModel;
using LacteosMadeline.Helpers;

namespace LacteosMadeline.Models;

/// <summary>
/// Línea de una venta. Conserva el nombre, la unidad, el precio y el costo
/// unitarios aplicados al momento de la transacción, para que el historial
/// y la utilidad no cambien aunque el producto se modifique posteriormente
/// (regla RN-08 de la documentación).
/// </summary>
public class DetalleVenta
{
    public int IdDetalleVenta { get; set; }
    public int IdVenta { get; set; }
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = UnidadMedida.Unidad;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Dinero.Redondear(PrecioUnitario * Cantidad);

    /// <summary>Costo del producto al momento de la venta (para el reporte de utilidad).</summary>
    [Browsable(false)]
    public decimal? CostoUnitario { get; set; }
}
