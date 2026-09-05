using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para la tabla Categorias.
/// </summary>
public class CategoriaRepository
{
    public int Registrar(Categoria categoria)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Categorias (Nombre, Estado)
            VALUES ($nombre, $estado);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nombre", categoria.Nombre);
        command.Parameters.AddWithValue("$estado", categoria.Estado ? 1 : 0);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Modificar(Categoria categoria)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Categorias
            SET Nombre = $nombre, Estado = $estado
            WHERE IdCategoria = $id;
            """;
        command.Parameters.AddWithValue("$nombre", categoria.Nombre);
        command.Parameters.AddWithValue("$estado", categoria.Estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", categoria.IdCategoria);
        command.ExecuteNonQuery();
    }

    public void CambiarEstado(int idCategoria, bool estado)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Categorias SET Estado = $estado WHERE IdCategoria = $id;";
        command.Parameters.AddWithValue("$estado", estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", idCategoria);
        command.ExecuteNonQuery();
    }

    public List<Categoria> ObtenerTodas(bool incluirInactivas = true)
    {
        var categorias = new List<Categoria>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = incluirInactivas
            ? "SELECT IdCategoria, Nombre, Estado FROM Categorias ORDER BY Nombre;"
            : "SELECT IdCategoria, Nombre, Estado FROM Categorias WHERE Estado = 1 ORDER BY Nombre;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categorias.Add(new Categoria
            {
                IdCategoria = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Estado = reader.GetInt32(2) == 1
            });
        }

        return categorias;
    }

    public Categoria? ObtenerPorId(int idCategoria)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT IdCategoria, Nombre, Estado FROM Categorias WHERE IdCategoria = $id;";
        command.Parameters.AddWithValue("$id", idCategoria);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Categoria
            {
                IdCategoria = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Estado = reader.GetInt32(2) == 1
            };
        }

        return null;
    }

    /// <summary>
    /// Verifica si ya existe una categoría con el mismo nombre (sin distinguir
    /// mayúsculas/minúsculas), excluyendo opcionalmente la categoría actual
    /// cuando se está modificando un registro existente.
    /// </summary>
    public bool ExisteNombre(string nombre, int idExcluir = 0)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM Categorias
            WHERE LOWER(Nombre) = LOWER($nombre) AND IdCategoria <> $idExcluir;
            """;
        command.Parameters.AddWithValue("$nombre", nombre);
        command.Parameters.AddWithValue("$idExcluir", idExcluir);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
