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
            EliminarRespaldosAntiguos(CarpetaRespaldosAutomaticos);

            var copia = CopiarAExterna(ruta);
            if (copia is not null)
            {
                return (true, $"{resultado.Detalle}\n{copia}");
            }
        }

        return resultado;
    }

    /// <summary>
    /// Copia el respaldo a la carpeta externa configurada (USB, OneDrive…).
    /// Si no hay carpeta configurada devuelve null; si la carpeta no está
    /// disponible (USB desconectada) lo informa sin anular el respaldo local.
    /// </summary>
    private static string? CopiarAExterna(string rutaRespaldo)
    {
        var carpeta = ConfiguracionNegocio.CarpetaRespaldoExterna;
        if (string.IsNullOrWhiteSpace(carpeta))
        {
            return null;
        }

        try
        {
            if (!Directory.Exists(carpeta))
            {
                LogHelper.Advertencia("Copia externa de respaldo", $"Carpeta no disponible: {carpeta}");
                return $"ATENCIÓN: la carpeta de copia externa no está disponible ({carpeta}). Conecte la USB o revise la ruta.";
            }

            var destino = Path.Combine(carpeta, Path.GetFileName(rutaRespaldo));
            File.Copy(rutaRespaldo, destino, overwrite: true);
            EliminarRespaldosAntiguos(carpeta);
            return $"Copia externa guardada en:\n{destino}";
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Copia externa de respaldo hacia {carpeta}", ex);
            return $"ATENCIÓN: no se pudo copiar el respaldo a la carpeta externa: {ex.Message}";
        }
    }

    /// <summary>
    /// Reemplaza la base de datos activa por un respaldo. Antes verifica que
    /// el archivo sea una base de Lácteos Madeline íntegra y no de una versión
    /// más nueva del programa, y guarda un respaldo de la base actual por si
    /// hay que volver atrás. El respaldo restaurado se actualiza a la versión
    /// actual. Después de restaurar hay que reiniciar el programa.
    /// </summary>
    public static (bool Exitoso, string Detalle) RestaurarDesde(string rutaRespaldo)
    {
        if (string.IsNullOrWhiteSpace(rutaRespaldo) || !File.Exists(rutaRespaldo))
        {
            return (false, "El archivo de respaldo no existe.");
        }

        var activo = Path.GetFullPath(DatabaseConnection.DatabasePath);
        if (string.Equals(Path.GetFullPath(rutaRespaldo), activo, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Ese archivo es la base de datos que está en uso.");
        }

        var validacion = ValidarArchivoRespaldo(rutaRespaldo);
        if (!validacion.EsValido)
        {
            return (false, validacion.Detalle);
        }

        var seguridad = RealizarBackupAutomatico("antes-de-restaurar");
        if (!seguridad.Exitoso)
        {
            return (false, $"No se restauró: no se pudo respaldar primero la base actual.\n{seguridad.Detalle}");
        }

        try
        {
            SqliteConnection.ClearAllPools();
            foreach (var sufijo in new[] { "-wal", "-shm" })
            {
                if (File.Exists(activo + sufijo)) File.Delete(activo + sufijo);
            }

            File.Copy(rutaRespaldo, activo, overwrite: true);
            DatabaseInitializer.Inicializar();
            LogHelper.Info("Restauración de respaldo", $"Base restaurada desde {rutaRespaldo}");
            return (true, "La base de datos se restauró correctamente. El programa se reiniciará.\n\n" +
                          $"La base anterior quedó respaldada en {CarpetaRespaldosAutomaticos}.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Restaurar respaldo desde {rutaRespaldo}", ex);
            return (false, "No se pudo completar la restauración. La base anterior quedó respaldada en " +
                           $"{CarpetaRespaldosAutomaticos} (archivo más reciente \"antes-de-restaurar\").\nDetalle: {ex.Message}");
        }
    }

    private static (bool EsValido, string Detalle) ValidarArchivoRespaldo(string ruta)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={ruta};Mode=ReadOnly;Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT (SELECT quick_check FROM pragma_quick_check),
                       (SELECT user_version FROM pragma_user_version),
                       (SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Productos', 'Ventas', 'Cajas'));
                """;
            using var reader = command.ExecuteReader();
            reader.Read();

            if (!string.Equals(reader.GetString(0), "ok", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "El archivo está dañado: no pasó la verificación de integridad.");
            }

            if (reader.GetInt64(2) < 3)
            {
                return (false, "El archivo no es un respaldo de Lácteos Madeline.");
            }

            if (reader.GetInt64(1) > DatabaseInitializer.VersionEsquema)
            {
                return (false, "El respaldo es de una versión más reciente del programa. Instale la versión más reciente para restaurarlo.");
            }

            return (true, string.Empty);
        }
        catch (SqliteException)
        {
            return (false, "El archivo no es una base de datos válida.");
        }
    }

    /// <summary>
    /// Respaldo verificado antes de actualizar la estructura de la base de
    /// datos. Va a Respaldos\Migraciones, fuera de la limpieza automática,
    /// porque es el punto de retorno si una actualización sale mal.
    /// </summary>
    public static (bool Exitoso, string Detalle, string Ruta) RealizarBackupAntesDeMigrar(int versionActual, int versionDestino)
    {
        var ruta = Path.Combine(CarpetaRespaldosAutomaticos, "Migraciones",
            $"{PrefijoArchivo}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_v{versionActual}-a-v{versionDestino}.db");

        var resultado = RealizarBackup(ruta);
        if (resultado.Exitoso)
        {
            LogHelper.Info("Respaldo previo a migración", ruta);
        }

        return (resultado.Exitoso, resultado.Detalle, ruta);
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

    private static void EliminarRespaldosAntiguos(string carpeta)
    {
        try
        {
            var antiguos = new DirectoryInfo(carpeta)
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
            LogHelper.Error($"Limpieza de respaldos antiguos en {carpeta}", ex);
        }
    }
}
