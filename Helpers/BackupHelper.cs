using LacteosMadeline.Data;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Genera copias de seguridad de la base de datos SQLite usando el
/// comando VACUUM INTO, que crea un archivo compacto e idéntico sin
/// tocar ni bloquear la base de datos activa (sección 20 lineamientos).
/// </summary>
public static class BackupHelper
{
    /// <summary>
    /// Realiza el respaldo hacia la ruta indicada.
    /// Devuelve (true, rutaFinal) si fue exitoso o (false, mensaje de error).
    /// </summary>
    public static (bool Exitoso, string Detalle) RealizarBackup(string rutaDestino)
    {
        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            return (false, "La ruta de destino no puede estar vacía.");
        }

        // Evitar sobreescribir la base de datos activa
        var archivoActivo = Path.GetFullPath(ObtenerRutaDbActiva());
        var archivoDestino = Path.GetFullPath(rutaDestino);

        if (string.Equals(archivoActivo, archivoDestino, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "La ruta de destino no puede ser la misma que la base de datos activa.");
        }

        try
        {
            // Crear directorio destino si no existe
            var directorio = Path.GetDirectoryName(archivoDestino);
            if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            // VACUUM INTO crea una copia compacta y consistente en una sola operación
            using var connection = DatabaseConnection.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"VACUUM INTO '{archivoDestino.Replace("'", "''")}';";
            command.ExecuteNonQuery();

            return (true, $"Respaldo guardado en:\n{archivoDestino}");
        }
        catch (Exception ex)
        {
            return (false, $"Error al realizar el respaldo:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Genera un nombre de archivo sugerido con fecha y hora actuales.
    /// Ejemplo: LacteosMadeline_backup_2026-09-05_15-30-00.db
    /// </summary>
    public static string GenerarNombreArchivo()
    {
        var marca = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return $"LacteosMadeline_backup_{marca}.db";
    }

    private static string ObtenerRutaDbActiva()
    {
        // La cadena de conexión tiene el formato "Data Source=<ruta>"
        var cs = DatabaseConnection.ConnectionString;
        const string prefix = "Data Source=";
        var idx = cs.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? cs[(idx + prefix.Length)..] : cs;
    }
}
