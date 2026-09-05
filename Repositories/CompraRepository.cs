using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para Compras y su detalle. El registro de la compra,
/// el detalle y el aumento de inventario ocurren dentro de una única
/// transacción para garantizar integridad total (sección 24 lineamientos):
///   Compra guardada → Detalle guardado → Inventario actualizado
/// Si cualquier paso falla se hace Rollback y nada queda guardado.
/// </summary>
public class CompraRepository
{
    public int RegistrarCompraCompleta(Compra compra)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var idCompra = InsertarCompra(connection, transaction, compra);

            foreach (var detalle in compra.Detalles)
            {
                InsertarDetalle(connection, transaction, idCompra, detalle);
                AumentarExistencia(connection, transaction, detalle.IdProducto, detalle.Cantidad);
            }

            transaction.Commit();
            return idCompra;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static int InsertarCompra(SqliteConnection connection, SqliteTransaction transaction, Compra compra)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Compras (FechaHora, IdProveedor, Total)
            VALUES ($fechaHora, $idProveedor, $total);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaHora", compra.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$idProveedor", compra.IdProveedor);
        command.Parameters.AddWithValue("$total", compra.Total);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertarDetalle(SqliteConnection connection, SqliteTransaction transaction, int idCompra, DetalleCompra detalle)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO DetalleCompras (IdCompra, IdProducto, NombreProducto, Cantidad, CostoUnitario, Subtotal)
            VALUES ($idCompra, $idProducto, $nombreProducto, $cantidad, $costoUnitario, $subtotal);
            """;
        command.Parameters.AddWithValue("$idCompra", idCompra);
        command.Parameters.AddWithValue("$idProducto", detalle.IdProducto);
        command.Parameters.AddWithValue("$nombreProducto", detalle.NombreProducto);
        command.Parameters.AddWithValue("$cantidad", detalle.Cantidad);
        command.Parameters.AddWithValue("$costoUnitario", detalle.CostoUnitario);
        command.Parameters.AddWithValue("$subtotal", detalle.Subtotal);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Aumenta la existencia del producto dentro de la transacción activa.
    /// Es la operación inversa a DescontarExistencia en ProductoRepository.
    /// </summary>
    private static void AumentarExistencia(SqliteConnection connection, SqliteTransaction transaction, int idProducto, int cantidad)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Productos
            SET Existencia = Existencia + $cantidad
            WHERE IdProducto = $id;
            """;
        command.Parameters.AddWithValue("$cantidad", cantidad);
        command.Parameters.AddWithValue("$id", idProducto);
        command.ExecuteNonQuery();
    }

    public Compra? ObtenerPorId(int idCompra)
    {
        using var connection = DatabaseConnection.CreateConnection();

        Compra? compra = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT c.IdCompra, c.FechaHora, c.IdProveedor, p.Nombre, c.Total
                FROM Compras c
                INNER JOIN Proveedores p ON p.IdProveedor = c.IdProveedor
                WHERE c.IdCompra = $id;
                """;
            command.Parameters.AddWithValue("$id", idCompra);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                compra = new Compra
                {
                    IdCompra       = reader.GetInt32(0),
                    FechaHora      = DateTime.Parse(reader.GetString(1)),
                    IdProveedor    = reader.GetInt32(2),
                    NombreProveedor = reader.GetString(3),
                    Total          = reader.GetDecimal(4)
                };
            }
        }

        if (compra is null) return null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT IdDetalleCompra, IdCompra, IdProducto, NombreProducto, Cantidad, CostoUnitario
                FROM DetalleCompras
                WHERE IdCompra = $id;
                """;
            command.Parameters.AddWithValue("$id", idCompra);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                compra.Detalles.Add(new DetalleCompra
                {
                    IdDetalleCompra = reader.GetInt32(0),
                    IdCompra        = reader.GetInt32(1),
                    IdProducto      = reader.GetInt32(2),
                    NombreProducto  = reader.GetString(3),
                    Cantidad        = reader.GetInt32(4),
                    CostoUnitario   = reader.GetDecimal(5)
                });
            }
        }

        return compra;
    }

    public List<Compra> ObtenerHistorial(DateTime? desde = null, DateTime? hasta = null)
    {
        var compras = new List<Compra>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.IdCompra, c.FechaHora, c.IdProveedor, p.Nombre, c.Total
            FROM Compras c
            INNER JOIN Proveedores p ON p.IdProveedor = c.IdProveedor
            WHERE ($desde IS NULL OR c.FechaHora >= $desde)
              AND ($hasta IS NULL OR c.FechaHora <= $hasta)
            ORDER BY c.IdCompra DESC;
            """;
        command.Parameters.AddWithValue("$desde", (object?)desde?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        command.Parameters.AddWithValue("$hasta", (object?)hasta?.ToString("yyyy-MM-dd 23:59:59") ?? DBNull.Value);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            compras.Add(new Compra
            {
                IdCompra        = reader.GetInt32(0),
                FechaHora       = DateTime.Parse(reader.GetString(1)),
                IdProveedor     = reader.GetInt32(2),
                NombreProveedor = reader.GetString(3),
                Total           = reader.GetDecimal(4)
            });
        }

        return compras;
    }
}
