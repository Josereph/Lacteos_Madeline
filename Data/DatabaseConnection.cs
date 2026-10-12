using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data;

/// <summary>
/// Centraliza la conexión a la base de datos SQLite del sistema.
/// Toda la aplicación debe obtener sus conexiones a través de esta clase
/// para evitar cadenas de conexión duplicadas o dispersas por el código.
/// </summary>
public static class DatabaseConnection
{
    // La variable de entorno LACTEOS_DB_PATH permite apuntar a otra base de
    // datos (por ejemplo, para pruebas) sin tocar la base de datos del negocio.
    public static readonly string DatabasePath =
        Environment.GetEnvironmentVariable("LACTEOS_DB_PATH") is { Length: > 0 } rutaPersonalizada
            ? Path.GetFullPath(rutaPersonalizada)
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LacteosMadeline",
                "LacteosMadeline.db");

    /// <summary>
    /// Carpeta de datos de la aplicación: base de datos, logs y respaldos.
    /// </summary>
    public static readonly string DatabaseFolder = Path.GetDirectoryName(DatabasePath)!;

    // Pooling: reutiliza conexiones abiertas, evitando reabrir el archivo
    // en cada consulta (importante para la velocidad en mostrador).
    public static string ConnectionString => $"Data Source={DatabasePath};Pooling=True";

    /// <summary>
    /// Crea y abre una nueva conexión a la base de datos.
    /// El llamador es responsable de liberarla (usar "using").
    /// </summary>
    public static SqliteConnection CreateConnection()
    {
        if (!Directory.Exists(DatabaseFolder))
        {
            Directory.CreateDirectory(DatabaseFolder);
        }

        var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        // Configuración por conexión:
        //  - foreign_keys: SQLite las desactiva por defecto; mantienen la
        //    integridad Producto/Venta/Caja.
        //  - synchronous = FULL: cada transacción confirmada queda en disco
        //    aunque se corte la luz o el programa se cierre abruptamente.
        //  - busy_timeout: si otra conexión (p. ej. un respaldo) tiene el
        //    archivo ocupado, espera hasta 5 s en lugar de fallar.
        // journal_mode = WAL se fija una sola vez en DatabaseInitializer
        // (es persistente en el archivo).
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = """
            PRAGMA foreign_keys = ON;
            PRAGMA synchronous = FULL;
            PRAGMA busy_timeout = 5000;
            """;
        pragmaCommand.ExecuteNonQuery();

        return connection;
    }
}
