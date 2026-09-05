using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para la tabla Productos. Las consultas incluyen el
/// nombre de la categoría mediante un JOIN para facilitar su uso directo
/// en la interfaz.
/// </summary>
public class ProductoRepository
{
    public int Registrar(Producto producto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Productos (Nombre, IdCategoria, PrecioCompra, PrecioVenta, Existencia, StockMinimo, Estado)
            VALUES ($nombre, $idCategoria, $precioCompra, $precioVenta, $existencia, $stockMinimo, $estado);
            SELECT last_insert_rowid();
            """;
        AgregarParametros(command, producto);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Modificar(Producto producto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Productos
            SET Nombre = $nombre,
                IdCategoria = $idCategoria,
                PrecioCompra = $precioCompra,
                PrecioVenta = $precioVenta,
                Existencia = $existencia,
                StockMinimo = $stockMinimo,
                Estado = $estado
            WHERE IdProducto = $id;
            """;
        AgregarParametros(command, producto);
        command.Parameters.AddWithValue("$id", producto.IdProducto);
        command.ExecuteNonQuery();
    }

    public void CambiarEstado(int idProducto, bool estado)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Productos SET Estado = $estado WHERE IdProducto = $id;";
        command.Parameters.AddWithValue("$estado", estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", idProducto);
        command.ExecuteNonQuery();
    }

    public List<Producto> Buscar(string filtro = "", bool soloActivos = false)
    {
        var productos = new List<Producto>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.IdProducto, p.Nombre, p.IdCategoria, c.Nombre, p.PrecioCompra,
                   p.PrecioVenta, p.Existencia, p.StockMinimo, p.Estado
            FROM Productos p
            INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria
            WHERE p.Nombre LIKE $filtro
              AND (($soloActivos = 0) OR (p.Estado = 1))
            ORDER BY p.Nombre;
            """;
        command.Parameters.AddWithValue("$filtro", $"%{filtro}%");
        command.Parameters.AddWithValue("$soloActivos", soloActivos ? 1 : 0);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(Mapear(reader));
        }

        return productos;
    }

    public Producto? ObtenerPorId(int idProducto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.IdProducto, p.Nombre, p.IdCategoria, c.Nombre, p.PrecioCompra,
                   p.PrecioVenta, p.Existencia, p.StockMinimo, p.Estado
            FROM Productos p
            INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria
            WHERE p.IdProducto = $id;
            """;
        command.Parameters.AddWithValue("$id", idProducto);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    /// <summary>
    /// Descuenta existencia dentro de una conexión y transacción ya abiertas.
    /// Se utiliza al confirmar una venta para que el descuento de inventario
    /// y el registro de la venta ocurran de forma atómica (todo o nada).
    /// La condición "Existencia >= $cantidad" en el propio UPDATE evita
    /// vender más unidades de las disponibles (regla RN-05).
    /// </summary>
    public void DescontarExistencia(SqliteConnection connection, SqliteTransaction transaction, int idProducto, int cantidad)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Productos
            SET Existencia = Existencia - $cantidad
            WHERE IdProducto = $id AND Existencia >= $cantidad;
            """;
        command.Parameters.AddWithValue("$cantidad", cantidad);
        command.Parameters.AddWithValue("$id", idProducto);

        var filasAfectadas = command.ExecuteNonQuery();
        if (filasAfectadas == 0)
        {
            throw new InvalidOperationException("Existencia insuficiente.");
        }
    }

    public void AjustarExistencia(int idProducto, int nuevaExistencia)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Productos SET Existencia = $existencia WHERE IdProducto = $id;";
        command.Parameters.AddWithValue("$existencia", nuevaExistencia);
        command.Parameters.AddWithValue("$id", idProducto);
        command.ExecuteNonQuery();
    }

    private static void AgregarParametros(SqliteCommand command, Producto producto)
    {
        command.Parameters.AddWithValue("$nombre", producto.Nombre);
        command.Parameters.AddWithValue("$idCategoria", producto.IdCategoria);
        command.Parameters.AddWithValue("$precioCompra", (object?)producto.PrecioCompra ?? DBNull.Value);
        command.Parameters.AddWithValue("$precioVenta", producto.PrecioVenta);
        command.Parameters.AddWithValue("$existencia", producto.Existencia);
        command.Parameters.AddWithValue("$stockMinimo", producto.StockMinimo);
        command.Parameters.AddWithValue("$estado", producto.Estado ? 1 : 0);
    }

    private static Producto Mapear(SqliteDataReader reader)
    {
        return new Producto
        {
            IdProducto = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            IdCategoria = reader.GetInt32(2),
            NombreCategoria = reader.GetString(3),
            PrecioCompra = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            PrecioVenta = reader.GetDecimal(5),
            Existencia = reader.GetInt32(6),
            StockMinimo = reader.GetInt32(7),
            Estado = reader.GetInt32(8) == 1
        };
    }
}
