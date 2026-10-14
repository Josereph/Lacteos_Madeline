using LacteosMadeline.Helpers;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data;

/// <summary>
/// Un cambio de estructura de la base de datos. <see cref="Version"/> es el
/// número que queda grabado en PRAGMA user_version una vez aplicado.
/// </summary>
public sealed record Migracion(int Version, string Descripcion, Action<SqliteConnection, SqliteTransaction> Aplicar);

/// <summary>
/// La base de datos no pudo llevarse a la versión que espera el programa.
/// El mensaje está pensado para mostrarse tal cual a la propietaria.
/// </summary>
public sealed class ErrorMigracionException : Exception
{
    public ErrorMigracionException(string mensaje, Exception? interna = null) : base(mensaje, interna) { }
}

/// <summary>
/// Aplica las migraciones pendientes según PRAGMA user_version:
///  1. Si la base pertenece a una versión MÁS NUEVA del programa, se detiene
///     sin tocar nada (evita que un programa antiguo dañe datos nuevos).
///  2. Si hay migraciones pendientes y la base ya tiene datos, genera un
///     respaldo verificado en Respaldos\Migraciones. Sin respaldo no migra.
///  3. Cada migración corre en su propia transacción junto con el cambio de
///     user_version: o se aplica completa y queda registrada, o se revierte
///     completa. Nunca queda un esquema a medias.
///  4. Antes de confirmar, PRAGMA foreign_key_check: una migración que deje
///     referencias rotas (p. ej. al reconstruir una tabla) se revierte.
/// Las llaves foráneas se desactivan durante la migración porque SQLite lo
/// exige para reconstruir tablas; la verificación del punto 4 las sustituye.
/// </summary>
public static class MigradorBaseDatos
{
    internal static void Aplicar(IReadOnlyList<Migracion> migraciones)
    {
        ValidarSecuencia(migraciones);
        var versionDestino = migraciones.Count == 0 ? 0 : migraciones[^1].Version;

        using var connection = DatabaseConnection.CreateConnection();
        var versionActual = ObtenerVersion(connection, null);

        if (versionActual > versionDestino)
        {
            throw new ErrorMigracionException(
                $"La base de datos pertenece a una versión más reciente del programa (estructura v{versionActual}; " +
                $"este programa llega hasta v{versionDestino}). Instale la versión más reciente. No se modificó ningún dato.");
        }

        if (versionActual == versionDestino)
        {
            return;
        }

        string? rutaRespaldo = null;
        if (TieneTablas(connection))
        {
            var respaldo = BackupHelper.RealizarBackupAntesDeMigrar(versionActual, versionDestino);
            if (!respaldo.Exitoso)
            {
                throw new ErrorMigracionException(
                    "No se pudo generar el respaldo previo a la actualización de la base de datos, " +
                    $"por lo que no se aplicó ningún cambio.\n{respaldo.Detalle}");
            }

            rutaRespaldo = respaldo.Ruta;
        }

        EjecutarPragma(connection, "PRAGMA foreign_keys = OFF;");
        try
        {
            foreach (var migracion in migraciones.Where(m => m.Version > versionActual))
            {
                AplicarUna(connection, migracion, rutaRespaldo);
            }
        }
        finally
        {
            EjecutarPragma(connection, "PRAGMA foreign_keys = ON;");
        }
    }

    public static int ObtenerVersion(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void AplicarUna(SqliteConnection connection, Migracion migracion, string? rutaRespaldo)
    {
        using var transaction = connection.BeginTransaction();

        try
        {
            // Otra instancia del programa pudo aplicarla mientras tanto.
            if (ObtenerVersion(connection, transaction) >= migracion.Version)
            {
                return;
            }

            var violacionesAntes = ContarViolacionesLlaveForanea(connection, transaction);
            migracion.Aplicar(connection, transaction);
            var violacionesDespues = ContarViolacionesLlaveForanea(connection, transaction);

            // Solo se rechazan las referencias rotas que introduce la
            // migración: datos heredados no deben impedir abrir el sistema.
            if (violacionesDespues > violacionesAntes)
            {
                throw new InvalidOperationException(
                    $"La migración dejó {violacionesDespues - violacionesAntes} referencia(s) rota(s) entre tablas.");
            }

            EjecutarPragma(connection, $"PRAGMA user_version = {migracion.Version};", transaction);
            transaction.Commit();
            LogHelper.Info("Migración de base de datos", $"Aplicada v{migracion.Version}: {migracion.Descripcion}");
        }
        catch (Exception ex)
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // SQLite pudo haberla revertido ya por su cuenta.
            }

            LogHelper.Error($"Migración v{migracion.Version} ({migracion.Descripcion})", ex);

            var avisoRespaldo = rutaRespaldo is null
                ? string.Empty
                : $"\nRespaldo previo a la actualización: {rutaRespaldo}";

            throw new ErrorMigracionException(
                $"No se pudo actualizar la base de datos a la versión {migracion.Version} ({migracion.Descripcion}). " +
                $"Los cambios de esa versión se revirtieron y los datos quedaron como estaban.{avisoRespaldo}\n" +
                $"Detalle: {ex.Message}", ex);
        }
    }

    private static void ValidarSecuencia(IReadOnlyList<Migracion> migraciones)
    {
        for (var i = 0; i < migraciones.Count; i++)
        {
            if (migraciones[i].Version != i + 1)
            {
                throw new InvalidOperationException(
                    $"Las migraciones deben numerarse 1, 2, 3… sin saltos; la posición {i + 1} tiene la versión {migraciones[i].Version}.");
            }
        }
    }

    private static bool TieneTablas(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static int ContarViolacionesLlaveForanea(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM pragma_foreign_key_check;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void EjecutarPragma(SqliteConnection connection, string pragma, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = pragma;
        command.ExecuteNonQuery();
    }
}
