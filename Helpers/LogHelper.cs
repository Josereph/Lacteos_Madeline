using System.Text;
using LacteosMadeline.Data;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Registro local de errores en %LOCALAPPDATA%\LacteosMadeline\logs.txt
/// (junto a la base de datos). Guarda fecha, hora, contexto y la traza
/// completa de la excepción para diagnóstico técnico. Nunca lanza
/// excepciones: un fallo al escribir el log no debe interrumpir la venta.
/// Cuando el archivo supera 5 MB se renombra a logs.anterior.txt.
/// </summary>
public static class LogHelper
{
    private const long TamanoMaximoBytes = 5 * 1024 * 1024;
    private static readonly object Candado = new();

    public static string RutaLog => Path.Combine(DatabaseConnection.DatabaseFolder, "logs.txt");

    public static void Error(string contexto, Exception ex)
    {
        Escribir("ERROR", contexto, ex.ToString());
    }

    public static void Advertencia(string contexto, string mensaje)
    {
        Escribir("AVISO", contexto, mensaje);
    }

    public static void Info(string contexto, string mensaje)
    {
        Escribir("INFO", contexto, mensaje);
    }

    private static void Escribir(string nivel, string contexto, string detalle)
    {
        try
        {
            var entrada = new StringBuilder()
                .Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ")
                .Append(nivel).Append(" | ").AppendLine(contexto)
                .AppendLine(detalle)
                .AppendLine(new string('-', 80))
                .ToString();

            lock (Candado)
            {
                Directory.CreateDirectory(DatabaseConnection.DatabaseFolder);
                RotarSiEsNecesario();
                AgregarConReintentos(entrada);
            }
        }
        catch
        {
            // El log es auxiliar: si el disco falla, la operación principal sigue.
        }
    }

    /// <summary>
    /// El antivirus o el indexador de Windows pueden bloquear el archivo un
    /// instante justo después de cada escritura. Se abre en modo compartido
    /// y se reintenta brevemente en lugar de perder la entrada.
    /// </summary>
    private static void AgregarConReintentos(string entrada)
    {
        var bytes = Encoding.UTF8.GetBytes(entrada);

        for (var intento = 1; ; intento++)
        {
            try
            {
                using var archivo = new FileStream(RutaLog, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                archivo.Write(bytes);
                return;
            }
            catch (IOException) when (intento < 5)
            {
                Thread.Sleep(20 * intento);
            }
        }
    }

    private static void RotarSiEsNecesario()
    {
        var archivo = new FileInfo(RutaLog);
        if (archivo.Exists && archivo.Length > TamanoMaximoBytes)
        {
            File.Move(RutaLog, Path.Combine(DatabaseConnection.DatabaseFolder, "logs.anterior.txt"), overwrite: true);
        }
    }
}
