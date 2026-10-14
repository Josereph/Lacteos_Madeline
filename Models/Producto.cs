using System.ComponentModel;

namespace LacteosMadeline.Models;

/// <summary>
/// Producto comercializado por Lácteos Madeline. Incluye la existencia
/// actual como parte del propio producto, tal como lo define el
/// diccionario de datos de la documentación (no existe una tabla de
/// inventario separada en esta primera etapa).
/// Existencia y stock mínimo se expresan en la <see cref="Unidad"/> del
/// producto y admiten decimales si la unidad es de peso o volumen.
/// </summary>
public class Producto
{
    public int IdProducto { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdCategoria { get; set; }
    public string? NombreCategoria { get; set; }
    public string Unidad { get; set; } = UnidadMedida.Unidad;
    public decimal? PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Existencia { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Estado { get; set; } = true;

    [Browsable(false)]
    public bool PermiteDecimales => UnidadMedida.PermiteDecimales(Unidad);
}
