namespace LacteosMadeline.Models;

/// <summary>
/// Producto comercializado por Lácteos Madeline. Incluye la existencia
/// actual como parte del propio producto, tal como lo define el
/// diccionario de datos de la documentación (no existe una tabla de
/// inventario separada en esta primera etapa).
/// </summary>
public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdCategoria { get; set; }
    public string? NombreCategoria { get; set; }
    public decimal? PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Existencia { get; set; }
    public int StockMinimo { get; set; }
    public bool Estado { get; set; } = true;
}
