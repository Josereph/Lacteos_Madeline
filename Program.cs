using LacteosMadeline.Data;

namespace LacteosMadeline;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // La base de datos y sus tablas se crean automáticamente la
        // primera vez que se ejecuta el sistema.
        DatabaseInitializer.Inicializar();

        Application.Run(new Form1());
    }
}
