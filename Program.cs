using LacteosMadeline.Data;
using LacteosMadeline.Helpers;

namespace LacteosMadeline;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Montos y fechas con el mismo formato en cualquier PC, sin depender
        // de la configuración regional de Windows (ver ConfiguracionNegocio).
        ConfiguracionNegocio.AplicarCultura();

        // Cualquier error no controlado se muestra con un mensaje claro en
        // lugar de cerrar el programa. Las operaciones de guardado usan
        // transacciones, por lo que un error nunca deja datos a medias.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => MostrarErrorInesperado(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) MostrarErrorInesperado(ex);
        };

        // La base de datos y sus tablas se crean automáticamente la
        // primera vez que se ejecuta el sistema.
        try
        {
            DatabaseInitializer.Inicializar();
        }
        catch (Exception ex)
        {
            LogHelper.Error("Inicialización de la base de datos", ex);
            MessageBox.Show(
                "No se pudo abrir la base de datos del sistema. El programa se cerrará.\n\n" +
                $"Detalle: {ex.Message}\n\n" +
                $"Carpeta de datos: {DatabaseConnection.DatabaseFolder}\n" +
                $"Registro de errores: {LogHelper.RutaLog}",
                "Lácteos Madeline – Error al iniciar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new Form1());

        MantenimientoAlSalir();
    }

    /// <summary>
    /// Al cerrar el programa: PRAGMA optimize actualiza las estadísticas
    /// que usa SQLite para elegir índices, y el checkpoint integra el
    /// archivo WAL en la base principal para que el .db quede completo
    /// por sí solo (útil si se copia el archivo manualmente).
    /// </summary>
    private static void MantenimientoAlSalir()
    {
        try
        {
            using var connection = DatabaseConnection.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA optimize; PRAGMA wal_checkpoint(TRUNCATE);";
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            LogHelper.Error("Mantenimiento al salir", ex);
        }
    }

    private static void MostrarErrorInesperado(Exception ex)
    {
        LogHelper.Error("Excepción no controlada", ex);

        var mensaje = ex is Microsoft.Data.Sqlite.SqliteException
            ? "Ocurrió un problema al acceder a la base de datos. La última operación no se guardó."
            : "Ocurrió un error inesperado. La última operación pudo no completarse.";

        MessageBox.Show(
            $"{mensaje}\n\nPuede seguir usando el sistema. Si el problema se repite, " +
            $"anote este detalle para soporte técnico:\n{ex.Message}\n\n" +
            $"El detalle completo quedó guardado en:\n{LogHelper.RutaLog}",
            "Lácteos Madeline – Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
