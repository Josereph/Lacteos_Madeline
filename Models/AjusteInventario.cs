namespace LacteosMadeline.Models;

/// <summary>
/// Corrección manual de la existencia de un producto (conteo físico o
/// corrección desde Productos), con el valor anterior, el nuevo y el motivo.
/// </summary>
public class AjusteInventario
{
    public int IdAjuste { get; set; }
    public DateTime FechaHora { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal ExistenciaAnterior { get; set; }
    public decimal ExistenciaNueva { get; set; }
    public decimal Diferencia => ExistenciaNueva - ExistenciaAnterior;
    public string Motivo { get; set; } = string.Empty;
    public string? NombreUsuario { get; set; }
}
