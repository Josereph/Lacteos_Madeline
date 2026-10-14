using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
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
            INSERT INTO Ventas (FechaHora, Total, MetodoPago, MontoRecibido, Cambio, IdCaja)
            VALUES ($fechaHora, $total, $metodoPago, $montoRecibido, $cambio, $idCaja);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(venta.FechaHora));
        command.Parameters.AddWithValue("$total", Dinero.ACentavos(venta.Total));
        command.Parameters.AddWithValue("$metodoPago", venta.MetodoPago);
        command.Parameters.AddWithValue("$montoRecibido", Dinero.ACentavos(venta.MontoRecibido));
        command.Parameters.AddWithValue("$cambio", Dinero.ACentavos(venta.Cambio));
        command.Parameters.AddWithValue("$idCaja", (object?)venta.IdCaja ?? DBNull.Value);

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
        command.Parameters.AddWithValue("$precioUnitario", Dinero.ACentavos(detalle.PrecioUnitario));
        command.Parameters.AddWithValue("$subtotal", Dinero.ACentavos(detalle.Subtotal));
        command.ExecuteNonQuery();
    }

    public Venta? ObtenerPorId(int idVenta)
    {
        using var connection = DatabaseConnection.CreateConnection();

        Venta? venta = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT {ColumnasVenta} FROM Ventas WHERE IdVenta = $id;";
            command.Parameters.AddWithValue("$id", idVenta);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                venta = MapearVenta(reader);
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
                    PrecioUnitario = reader.LeerDinero(5)
                });
            }
        }

        return venta;
    }

    /// <summary>
    /// Ventas del rango de fechas (días completos), más recientes primero,
    /// o en orden cronológico si <paramref name="cronologico"/> es true.
    /// Usa el índice IX_Ventas_FechaHora: no recorre todo el historial.
    /// </summary>
    public List<Venta> ObtenerPorRango(DateTime? desde, DateTime? hasta, bool cronologico = false)
    {
        var ventas = new List<Venta>();
        var orden = cronologico ? "FechaHora, IdVenta" : "FechaHora DESC, IdVenta DESC";

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnasVenta} FROM Ventas
            WHERE FechaHora BETWEEN $desde AND $hasta
            ORDER BY {orden};
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ventas.Add(MapearVenta(reader));
        }

        return ventas;
    }

    public List<Venta> ObtenerHistorial()
    {
        var ventas = new List<Venta>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {ColumnasVenta} FROM Ventas ORDER BY IdVenta DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ventas.Add(MapearVenta(reader));
        }

        return ventas;
    }

    /// <summary>
    /// Columnas de Ventas en el orden que espera MapearVenta.
    /// </summary>
    public const string ColumnasVenta = "IdVenta, FechaHora, Total, MetodoPago, MontoRecibido, Cambio, IdCaja";

    public static Venta MapearVenta(SqliteDataReader reader)
    {
        return new Venta
        {
            IdVenta = reader.GetInt32(0),
            FechaHora = Fechas.DesdeBaseDatos(reader.GetString(1)),
            Total = reader.LeerDinero(2),
            MetodoPago = reader.GetString(3),
            MontoRecibido = reader.LeerDineroONulo(4),
            Cambio = reader.LeerDineroONulo(5),
            IdCaja = reader.IsDBNull(6) ? null : reader.GetInt32(6)
        };
    }
}
