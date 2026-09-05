namespace LacteosMadeline.Models;

/// <summary>
/// Línea de una compra. Conserva NombreProducto y CostoUnitario
/// históricos para que los registros anteriores no cambien si el
/// producto se modifica en el futuro (sección 25 lineamientos).
/// </summary>
public class DetalleCompra
{
    public int IdDetalleCompra { get; set; }
    public int IdCompra { get; set; }
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal => CostoUnitario * Cantidad;
}
