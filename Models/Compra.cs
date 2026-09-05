namespace LacteosMadeline.Models;

/// <summary>
/// Cabecera de una compra. Guarda el nombre del proveedor en el momento
/// de la compra para conservar el historial aunque el proveedor cambie
/// de nombre después (sección 25 lineamientos).
/// </summary>
public class Compra
{
    public int IdCompra { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public int IdProveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<DetalleCompra> Detalles { get; set; } = new();
}
