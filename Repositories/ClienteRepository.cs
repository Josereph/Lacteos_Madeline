using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para la tabla Clientes. Sigue el patrón de
/// ProveedorRepository: los clientes se desactivan, no se eliminan.
/// </summary>
public class ClienteRepository
{
    private const string Columnas = "IdCliente, Nombre, Documento, Telefono, Direccion, Correo, Estado";

    public int Registrar(Cliente cliente)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Clientes (Nombre, Documento, Telefono, Direccion, Correo, Estado)
            VALUES ($nombre, $documento, $telefono, $direccion, $correo, $estado);
            SELECT last_insert_rowid();
            """;
        AgregarParametros(command, cliente);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Modificar(Cliente cliente)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Clientes
            SET Nombre = $nombre, Documento = $documento, Telefono = $telefono,
                Direccion = $direccion, Correo = $correo, Estado = $estado
            WHERE IdCliente = $id;
            """;
        AgregarParametros(command, cliente);
        command.Parameters.AddWithValue("$id", cliente.IdCliente);
        command.ExecuteNonQuery();
    }

    public void CambiarEstado(int idCliente, bool estado)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Clientes SET Estado = $estado WHERE IdCliente = $id;";
        command.Parameters.AddWithValue("$estado", estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", idCliente);
        command.ExecuteNonQuery();
    }

    /// <summary>Busca por nombre, documento o teléfono.</summary>
    public List<Cliente> Buscar(string filtro = "", bool soloActivos = false)
    {
        var clientes = new List<Cliente>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {Columnas} FROM Clientes
            WHERE (Nombre LIKE $filtro OR Documento LIKE $filtro OR Telefono LIKE $filtro)
              AND ($soloActivos = 0 OR Estado = 1)
            ORDER BY Nombre;
            """;
        command.Parameters.AddWithValue("$filtro", $"%{filtro}%");
        command.Parameters.AddWithValue("$soloActivos", soloActivos ? 1 : 0);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            clientes.Add(Mapear(reader));
        }

        return clientes;
    }

    public Cliente? ObtenerPorId(int idCliente)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columnas} FROM Clientes WHERE IdCliente = $id;";
        command.Parameters.AddWithValue("$id", idCliente);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    private static void AgregarParametros(SqliteCommand command, Cliente cliente)
    {
        command.Parameters.AddWithValue("$nombre", cliente.Nombre);
        command.Parameters.AddWithValue("$documento", (object?)cliente.Documento ?? DBNull.Value);
        command.Parameters.AddWithValue("$telefono", (object?)cliente.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("$direccion", (object?)cliente.Direccion ?? DBNull.Value);
        command.Parameters.AddWithValue("$correo", (object?)cliente.Correo ?? DBNull.Value);
        command.Parameters.AddWithValue("$estado", cliente.Estado ? 1 : 0);
    }

    private static Cliente Mapear(SqliteDataReader reader) => new()
    {
        IdCliente = reader.GetInt32(0),
        Nombre = reader.GetString(1),
        Documento = reader.IsDBNull(2) ? null : reader.GetString(2),
        Telefono = reader.IsDBNull(3) ? null : reader.GetString(3),
        Direccion = reader.IsDBNull(4) ? null : reader.GetString(4),
        Correo = reader.IsDBNull(5) ? null : reader.GetString(5),
        Estado = reader.GetInt32(6) == 1
    };
}
