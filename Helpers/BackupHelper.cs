using LacteosMadeline.Data;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Genera copias de seguridad de la base de datos SQLite usando el
/// comando VACUUM INTO, que crea un archivo compacto y consistente sin
/// detener ni bloquear la base de datos activa (sección 20 lineamientos).
/// Cada respaldo se verifica (PRAGMA quick_check) antes de darlo por bueno.
/// Los respaldos automáticos (al cerrar caja) se guardan en
/// %LOCALAPPDATA%\LacteosMadeline\Respaldos y se conservan los más recientes.
/// </summary>
public static class BackupHelper
{
    private const string PrefijoArchivo = "LacteosMadeline_backup_";
    private const int RespaldosAutomaticosAConservar = 60;

    public static string CarpetaRespaldosAutomaticos =>
        Path.Combine(DatabaseConnection.DatabaseFolder, "Respaldos");

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
        var archivoActivo = Path.GetFullPath(DatabaseConnection.DatabasePath);
        var archivoDestino = Path.GetFullPath(rutaDestino);

        if (string.Equals(archivoActivo, archivoDestino, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "La ruta de destino no puede ser la misma que la base de datos activa.");
        }

        try
        {
            var directorio = Path.GetDirectoryName(archivoDestino);
            if (!string.IsNullOrEmpty(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            // VACUUM INTO falla si el archivo ya existe.
            if (File.Exists(archivoDestino))
            {
                File.Delete(archivoDestino);
            }

            using (var connection = DatabaseConnection.CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "VACUUM INTO $destino;";
                command.Parameters.AddWithValue("$destino", archivoDestino);
                command.ExecuteNonQuery();
            }

            if (!VerificarRespaldo(archivoDestino))
            {
                LogHelper.Advertencia("Respaldo", $"El respaldo no pasó la verificación de integridad: {archivoDestino}");
                return (false, "El respaldo se generó, pero no pasó la verificación de integridad. Intente de nuevo.");
            }

            return (true, $"Respaldo guardado en:\n{archivoDestino}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Respaldo hacia {archivoDestino}", ex);
            return (false, $"Error al realizar el respaldo:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Respaldo automático con marca de fecha y hora, por ejemplo
    /// LacteosMadeline_backup_2026-10-12_18-30-05_cierre-caja-15.db.
    /// Elimina los respaldos automáticos más antiguos para no llenar el disco.
    /// </summary>
    public static (bool Exitoso, string Detalle) RealizarBackupAutomatico(string motivo)
    {
        var etiqueta = string.Concat(motivo.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        var ruta = Path.Combine(CarpetaRespaldosAutomaticos, $"{PrefijoArchivo}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{etiqueta}.db");

        var resultado = RealizarBackup(ruta);
        if (resultado.Exitoso)
        {
            LogHelper.Info("Respaldo automático", ruta);
            EliminarRespaldosAntiguos();
        }

        return resultado;
    }

    /// <summary>
    /// Genera un nombre de archivo sugerido con fecha y hora actuales.
    /// Ejemplo: LacteosMadeline_backup_2026-09-05_15-30-00.db
    /// </summary>
    public static string GenerarNombreArchivo()
    {
        var marca = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return $"{PrefijoArchivo}{marca}.db";
    }

    private static bool VerificarRespaldo(string ruta)
    {
        using var connection = new SqliteConnection($"Data Source={ruta};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check;";
        return string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.OrdinalIgnoreCase);
    }

    private static void EliminarRespaldosAntiguos()
    {
        try
        {
            var antiguos = new DirectoryInfo(CarpetaRespaldosAutomaticos)
                .GetFiles($"{PrefijoArchivo}*.db")
                .OrderByDescending(f => f.Name) // el nombre empieza con la fecha
                .Skip(RespaldosAutomaticosAConservar);

            foreach (var archivo in antiguos)
            {
                archivo.Delete();
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("Limpieza de respaldos antiguos", ex);
        }
    }
}
