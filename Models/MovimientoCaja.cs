namespace LacteosMadeline.Models;

/// <summary>
/// Movimiento manual de entrada o salida de efectivo dentro de una
/// jornada de caja (sección 12 de los lineamientos).
/// </summary>
public class MovimientoCaja
{
    public int IdMovimiento { get; set; }
    public int IdCaja { get; set; }
    public DateTime FechaHora { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Entrada" | "Salida"
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
}
