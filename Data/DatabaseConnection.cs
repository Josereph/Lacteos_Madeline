using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data;

/// <summary>
/// Centraliza la conexión a la base de datos SQLite del sistema.
/// Toda la aplicación debe obtener sus conexiones a través de esta clase
/// para evitar cadenas de conexión duplicadas o dispersas por el código.
/// </summary>
public static class DatabaseConnection
{
    private static readonly string DatabaseFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LacteosMadeline");

    private static readonly string DatabasePath = Path.Combine(DatabaseFolder, "LacteosMadeline.db");

    public static string ConnectionString => $"Data Source={DatabasePath}";

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

        // SQLite desactiva las llaves foráneas por defecto; se habilitan
        // en cada conexión para mantener la integridad Producto/Venta.
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCommand.ExecuteNonQuery();

        return connection;
    }
}
