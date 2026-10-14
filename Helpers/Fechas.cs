using System.Globalization;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Formato único de fechas en la base de datos: texto "yyyy-MM-dd HH:mm:ss"
/// en hora local, escrito y leído siempre con la cultura invariante. Así el
/// archivo se lee igual en cualquier PC, sin importar su configuración
/// regional (separadores de hora, calendario, orden día/mes).
/// </summary>
public static class Fechas
{
    public const string FormatoBaseDatos = "yyyy-MM-dd HH:mm:ss";

    public static string ParaBaseDatos(DateTime fecha) =>
        fecha.ToString(FormatoBaseDatos, CultureInfo.InvariantCulture);

    public static DateTime DesdeBaseDatos(string texto) =>
        DateTime.TryParseExact(texto, FormatoBaseDatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : DateTime.Parse(texto, CultureInfo.InvariantCulture); // registros antiguos con otro formato

    /// <summary>Inicio del día (00:00:00) para filtros por rango.</summary>
    public static string InicioDelDia(DateTime fecha) =>
        fecha.Date.ToString(FormatoBaseDatos, CultureInfo.InvariantCulture);

    /// <summary>Último segundo del día (23:59:59) para filtros por rango.</summary>
    public static string FinDelDia(DateTime fecha) =>
        fecha.Date.AddDays(1).AddSeconds(-1).ToString(FormatoBaseDatos, CultureInfo.InvariantCulture);
}
