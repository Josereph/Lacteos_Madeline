using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para Ventas y su detalle. Registrar la venta, guardar
/// el detalle y descontar el inventario se realiza dentro de una misma
/// transacción, de forma que una venta nunca deje el inventario en un
/// estado inconsistente (sección 7 de los lineamientos: Producto → Inventario → Venta).
/// </summary>
public class VentaRepository
{
    private readonly ProductoRepository _productoRepository = new();

    public int RegistrarVentaCompleta(Venta venta)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var idVenta = InsertarVenta(connection, transaction, venta);

            foreach (var detalle in venta.Detalles)
            {
                _productoRepository.DescontarExistencia(connection, transaction, detalle.IdProducto, detalle.Cantidad);
                InsertarDetalle(connection, transaction, idVenta, detalle);
            }

            transaction.Commit();
            return idVenta;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static int InsertarVenta(SqliteConnection connection, SqliteTransaction transaction, Venta venta)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Ventas (FechaHora, Total)
            VALUES ($fechaHora, $total);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaHora", venta.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$total", venta.Total);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertarDetalle(SqliteConnection connection, SqliteTransaction transaction, int idVenta, DetalleVenta detalle)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO DetalleVentas (IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal)
            VALUES ($idVenta, $idProducto, $nombreProducto, $cantidad, $precioUnitario, $subtotal);
            """;
        command.Parameters.AddWithValue("$idVenta", idVenta);
        command.Parameters.AddWithValue("$idProducto", detalle.IdProducto);
        command.Parameters.AddWithValue("$nombreProducto", detalle.NombreProducto);
        command.Parameters.AddWithValue("$cantidad", detalle.Cantidad);
        command.Parameters.AddWithValue("$precioUnitario", detalle.PrecioUnitario);
        command.Parameters.AddWithValue("$subtotal", detalle.Subtotal);
        command.ExecuteNonQuery();
    }

    public Venta? ObtenerPorId(int idVenta)
    {
        using var connection = DatabaseConnection.CreateConnection();

        Venta? venta = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT IdVenta, FechaHora, Total FROM Ventas WHERE IdVenta = $id;";
            command.Parameters.AddWithValue("$id", idVenta);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                venta = new Venta
                {
                    IdVenta = reader.GetInt32(0),
                    FechaHora = DateTime.Parse(reader.GetString(1)),
                    Total = reader.GetDecimal(2)
                };
            }
        }

        if (venta is null)
        {
            return null;
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT IdDetalleVenta, IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario
                FROM DetalleVentas
                WHERE IdVenta = $id;
                """;
            command.Parameters.AddWithValue("$id", idVenta);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                venta.Detalles.Add(new DetalleVenta
                {
                    IdDetalleVenta = reader.GetInt32(0),
                    IdVenta = reader.GetInt32(1),
                    IdProducto = reader.GetInt32(2),
                    NombreProducto = reader.GetString(3),
                    Cantidad = reader.GetInt32(4),
                    PrecioUnitario = reader.GetDecimal(5)
                });
            }
        }

        return venta;
    }

    public List<Venta> ObtenerHistorial()
    {
        var ventas = new List<Venta>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT IdVenta, FechaHora, Total FROM Ventas ORDER BY IdVenta DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ventas.Add(new Venta
            {
                IdVenta = reader.GetInt32(0),
                FechaHora = DateTime.Parse(reader.GetString(1)),
                Total = reader.GetDecimal(2)
            });
        }

        return ventas;
    }
}
