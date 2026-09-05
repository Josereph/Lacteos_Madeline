using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para la tabla Proveedores. Sigue el patrón de
/// CategoriaRepository: se prefiere desactivar antes de eliminar
/// cuando el proveedor ya tiene compras asociadas (sección 4 lineamientos).
/// </summary>
public class ProveedorRepository
{
    public int Registrar(Proveedor proveedor)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Proveedores (Nombre, Contacto, Telefono, Direccion, Observaciones, Estado)
            VALUES ($nombre, $contacto, $telefono, $direccion, $observaciones, $estado);
            SELECT last_insert_rowid();
            """;
        AgregarParametros(command, proveedor);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Modificar(Proveedor proveedor)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Proveedores
            SET Nombre        = $nombre,
                Contacto      = $contacto,
                Telefono      = $telefono,
                Direccion     = $direccion,
                Observaciones = $observaciones,
                Estado        = $estado
            WHERE IdProveedor = $id;
            """;
        AgregarParametros(command, proveedor);
        command.Parameters.AddWithValue("$id", proveedor.IdProveedor);
        command.ExecuteNonQuery();
    }

    public void CambiarEstado(int idProveedor, bool estado)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Proveedores SET Estado = $estado WHERE IdProveedor = $id;";
        command.Parameters.AddWithValue("$estado", estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", idProveedor);
        command.ExecuteNonQuery();
    }

    public List<Proveedor> Buscar(string filtro = "", bool soloActivos = false)
    {
        var proveedores = new List<Proveedor>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdProveedor, Nombre, Contacto, Telefono, Direccion, Observaciones, Estado
            FROM Proveedores
            WHERE Nombre LIKE $filtro
              AND (($soloActivos = 0) OR (Estado = 1))
            ORDER BY Nombre;
            """;
        command.Parameters.AddWithValue("$filtro", $"%{filtro}%");
        command.Parameters.AddWithValue("$soloActivos", soloActivos ? 1 : 0);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            proveedores.Add(Mapear(reader));
        }

        return proveedores;
    }

    public Proveedor? ObtenerPorId(int idProveedor)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdProveedor, Nombre, Contacto, Telefono, Direccion, Observaciones, Estado
            FROM Proveedores
            WHERE IdProveedor = $id;
            """;
        command.Parameters.AddWithValue("$id", idProveedor);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    /// <summary>
    /// Verifica si el proveedor tiene al menos una compra registrada,
    /// para evitar eliminar proveedores con historial (sección 21 lineamientos).
    /// </summary>
    public bool TieneCompras(int idProveedor)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Compras WHERE IdProveedor = $id;";
        command.Parameters.AddWithValue("$id", idProveedor);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void AgregarParametros(SqliteCommand command, Proveedor proveedor)
    {
        command.Parameters.AddWithValue("$nombre", proveedor.Nombre);
        command.Parameters.AddWithValue("$contacto", (object?)proveedor.Contacto ?? DBNull.Value);
        command.Parameters.AddWithValue("$telefono", (object?)proveedor.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("$direccion", (object?)proveedor.Direccion ?? DBNull.Value);
        command.Parameters.AddWithValue("$observaciones", (object?)proveedor.Observaciones ?? DBNull.Value);
        command.Parameters.AddWithValue("$estado", proveedor.Estado ? 1 : 0);
    }

    private static Proveedor Mapear(SqliteDataReader reader)
    {
        return new Proveedor
        {
            IdProveedor   = reader.GetInt32(0),
            Nombre        = reader.GetString(1),
            Contacto      = reader.IsDBNull(2) ? null : reader.GetString(2),
            Telefono      = reader.IsDBNull(3) ? null : reader.GetString(3),
            Direccion     = reader.IsDBNull(4) ? null : reader.GetString(4),
            Observaciones = reader.IsDBNull(5) ? null : reader.GetString(5),
            Estado        = reader.GetInt32(6) == 1
        };
    }
}
