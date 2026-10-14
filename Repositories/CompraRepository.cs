using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
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
    private readonly ProductoRepository _productoRepository = new();

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
                _productoRepository.RegistrarEntrada(connection, transaction, detalle.IdProducto, detalle.Cantidad, detalle.CostoUnitario);
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
            INSERT INTO Compras (FechaHora, IdProveedor, Total, IdUsuario)
            VALUES ($fechaHora, $idProveedor, $total, $idUsuario);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(compra.FechaHora));
        command.Parameters.AddWithValue("$idProveedor", compra.IdProveedor);
        command.Parameters.AddWithValue("$total", Dinero.ACentavos(compra.Total));
        command.Parameters.AddWithValue("$idUsuario", (object?)compra.IdUsuario ?? DBNull.Value);

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
        command.Parameters.AddWithValue("$cantidad", Cantidades.AMilesimas(detalle.Cantidad));
        command.Parameters.AddWithValue("$costoUnitario", Dinero.ACentavos(detalle.CostoUnitario));
        command.Parameters.AddWithValue("$subtotal", Dinero.ACentavos(detalle.Subtotal));
        command.ExecuteNonQuery();
    }

    public Compra? ObtenerPorId(int idCompra)
    {
        using var connection = DatabaseConnection.CreateConnection();

        Compra? compra = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT c.IdCompra, c.FechaHora, c.IdProveedor, p.Nombre, c.Total, u.Nombre
                FROM Compras c
                INNER JOIN Proveedores p ON p.IdProveedor = c.IdProveedor
                LEFT JOIN Usuarios u ON u.IdUsuario = c.IdUsuario
                WHERE c.IdCompra = $id;
                """;
            command.Parameters.AddWithValue("$id", idCompra);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                compra = new Compra
                {
                    IdCompra       = reader.GetInt32(0),
                    FechaHora      = Fechas.DesdeBaseDatos(reader.GetString(1)),
                    IdProveedor    = reader.GetInt32(2),
                    NombreProveedor = reader.GetString(3),
                    Total          = reader.LeerDinero(4),
                    NombreUsuario  = reader.IsDBNull(5) ? null : reader.GetString(5)
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
                    Cantidad        = reader.LeerCantidad(4),
                    CostoUnitario   = reader.LeerDinero(5)
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
            SELECT c.IdCompra, c.FechaHora, c.IdProveedor, p.Nombre, c.Total, u.Nombre
            FROM Compras c
            INNER JOIN Proveedores p ON p.IdProveedor = c.IdProveedor
            LEFT JOIN Usuarios u ON u.IdUsuario = c.IdUsuario
            WHERE c.FechaHora BETWEEN $desde AND $hasta
            ORDER BY c.IdCompra DESC;
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            compras.Add(new Compra
            {
                IdCompra        = reader.GetInt32(0),
                FechaHora       = Fechas.DesdeBaseDatos(reader.GetString(1)),
                IdProveedor     = reader.GetInt32(2),
                NombreProveedor = reader.GetString(3),
                Total           = reader.LeerDinero(4),
                NombreUsuario   = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return compras;
    }
}
