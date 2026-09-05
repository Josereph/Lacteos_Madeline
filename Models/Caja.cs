namespace LacteosMadeline.Models;

/// <summary>
/// Jornada de caja. Solo puede haber una caja con Estado "Abierta"
/// al mismo tiempo. El sistema evita aperturas duplicadas (sección 11
/// y validaciones de sección 23 de los lineamientos).
/// </summary>
public class Caja
{
    public int IdCaja { get; set; }
    public DateTime FechaApertura { get; set; }
    public decimal MontoInicial { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? MontoFinal { get; set; }
    public string Estado { get; set; } = "Abierta"; // "Abierta" | "Cerrada"
    public List<MovimientoCaja> Movimientos { get; set; } = new();
}
