using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Traduce errores de SQLite a mensajes claros para la propietaria.
/// Las reglas de integridad de la base (triggers RAISE ABORT) llegan como
/// SqliteException con código 19 (CONSTRAINT) y su mensaje ya está en
/// español; aquí se extrae el texto sin el prefijo técnico.
/// </summary>
public static class ErroresBaseDatos
{
    private const int SqliteConstraint = 19;

    public static bool EsReglaDeIntegridad(Exception ex) =>
        ex is SqliteException { SqliteErrorCode: SqliteConstraint };

    public static string MensajeAmigable(Exception ex)
    {
        if (ex is not SqliteException sqlEx)
        {
            return ex.Message;
        }

        // Formato: "SQLite Error 19: 'texto del trigger'."
        var mensaje = sqlEx.Message;
        var inicio = mensaje.IndexOf('\'');
        var fin = mensaje.LastIndexOf('\'');
        if (sqlEx.SqliteErrorCode == SqliteConstraint && inicio >= 0 && fin > inicio)
        {
            return mensaje[(inicio + 1)..fin];
        }

        return sqlEx.SqliteErrorCode switch
        {
            5 or 6 => "La base de datos está ocupada (por ejemplo, por un respaldo). Intente de nuevo en unos segundos.",
            10 or 11 or 26 => "Error de lectura/escritura en la base de datos. Restaure el último respaldo y contacte a soporte.",
            13 => "El disco está lleno. Libere espacio para seguir trabajando.",
            _ => sqlEx.Message
        };
    }
}
