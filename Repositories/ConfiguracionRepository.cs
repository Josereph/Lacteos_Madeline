using LacteosMadeline.Data;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Pares clave/valor de la tabla Configuracion (datos del negocio y
/// preferencias editables desde el programa).
/// </summary>
public class ConfiguracionRepository
{
    public Dictionary<string, string> ObtenerTodo()
    {
        var valores = new Dictionary<string, string>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Clave, Valor FROM Configuracion;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            valores[reader.GetString(0)] = reader.GetString(1);
        }

        return valores;
    }

    /// <summary>Guarda todos los valores en una sola transacción.</summary>
    public void Guardar(IReadOnlyDictionary<string, string> valores)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var (clave, valor) in valores)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Configuracion (Clave, Valor) VALUES ($clave, $valor)
                ON CONFLICT (Clave) DO UPDATE SET Valor = excluded.Valor;
                """;
            command.Parameters.AddWithValue("$clave", clave);
            command.Parameters.AddWithValue("$valor", valor);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
