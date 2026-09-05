namespace LacteosMadeline.Models;

/// <summary>
/// Línea de una venta. Conserva el nombre y el precio unitario aplicados
/// al momento de la transacción, para que el historial no cambie aunque
/// el producto se modifique posteriormente (regla RN-08 de la documentación).
/// </summary>
public class DetalleVenta
{
    public int IdDetalleVenta { get; set; }
    public int IdVenta { get; set; }
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
}
