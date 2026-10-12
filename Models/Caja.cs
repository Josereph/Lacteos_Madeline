namespace LacteosMadeline.Models;

/// <summary>
/// Jornada de caja. Solo puede haber una caja con Estado "Abierta"
/// al mismo tiempo. El sistema evita aperturas duplicadas (sección 11
/// y validaciones de sección 23 de los lineamientos).
/// MontoEsperado y Diferencia se guardan al cerrar, como registro del arqueo.
/// </summary>
public class Caja
{
    public int IdCaja { get; set; }
    public DateTime FechaApertura { get; set; }
    public decimal MontoInicial { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? MontoFinal { get; set; }
    public decimal? MontoEsperado { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = "Abierta"; // "Abierta" | "Cerrada"
    public List<MovimientoCaja> Movimientos { get; set; } = new();

    /// <summary>
    /// Única fórmula del efectivo esperado en caja, usada tanto en el
    /// resumen en vivo como en el cierre:
    ///   Fondo inicial + Ventas en efectivo + Entradas − Salidas
    /// Redondeado a centavos para evitar residuos de punto flotante.
    /// </summary>
    public static decimal CalcularSaldoEsperado(decimal montoInicial, decimal ventasEfectivo, decimal entradas, decimal salidas)
    {
        return Math.Round(montoInicial + ventasEfectivo + entradas - salidas, 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// Totales de una caja obtenidos con una sola consulta.
/// </summary>
public record TotalesCaja(
    decimal MontoInicial,
    string Estado,
    decimal VentasEfectivo,
    decimal VentasOtrosMetodos,
    decimal Entradas,
    decimal Salidas)
{
    public decimal SaldoEsperado => Caja.CalcularSaldoEsperado(MontoInicial, VentasEfectivo, Entradas, Salidas);
}
