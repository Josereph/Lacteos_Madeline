using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Parámetros $desde / $hasta para filtrar columnas FechaHora
/// (texto "yyyy-MM-dd HH:mm:ss") con "FechaHora BETWEEN $desde AND $hasta".
/// Siempre se envían ambos límites (si no hay filtro se usan extremos),
/// porque una condición como "($desde IS NULL OR FechaHora >= $desde)"
/// impide a SQLite usar el índice de FechaHora y obliga a recorrer toda
/// la tabla a medida que crece el historial.
/// </summary>
internal static class RangoFechas
{
    private const string Minimo = "0000-01-01 00:00:00";
    private const string Maximo = "9999-12-31 23:59:59";

    public static void AgregarParametros(SqliteCommand command, DateTime? desde, DateTime? hasta)
    {
        command.Parameters.AddWithValue("$desde", desde?.ToString("yyyy-MM-dd 00:00:00") ?? Minimo);
        command.Parameters.AddWithValue("$hasta", hasta?.ToString("yyyy-MM-dd 23:59:59") ?? Maximo);
    }
}
