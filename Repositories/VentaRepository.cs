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
/// Lo mismo ocurre al anularla: inventario, caja y estado cambian juntos.
/// </summary>
public class VentaRepository
{
    private readonly ProductoRepository _productoRepository = new();

    /// <summary>
    /// Columnas y uniones de una venta, en el orden que espera <see cref="MapearVenta"/>.
    /// </summary>
    private const string ConsultaVenta = """
        SELECT v.IdVenta, v.FechaHora, v.Total, v.MetodoPago, v.MontoRecibido, v.Cambio, v.IdCaja,
               v.Descuento, v.IdCliente, cl.Nombre, v.IdUsuario, u.Nombre, v.Estado,
               v.FechaAnulacion, v.MotivoAnulacion, ua.Nombre, v.IdCajaAnulacion, v.IdUsuarioDescuento
        FROM Ventas v
        LEFT JOIN Clientes cl ON cl.IdCliente = v.IdCliente
        LEFT JOIN Usuarios u  ON u.IdUsuario  = v.IdUsuario
        LEFT JOIN Usuarios ua ON ua.IdUsuario = v.IdUsuarioAnulacion
        """;

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
            INSERT INTO Ventas (FechaHora, Total, MetodoPago, MontoRecibido, Cambio, IdCaja, Descuento, IdCliente, IdUsuario, IdUsuarioDescuento)
            VALUES ($fechaHora, $total, $metodoPago, $montoRecibido, $cambio, $idCaja, $descuento, $idCliente, $idUsuario, $idUsuarioDescuento);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(venta.FechaHora));
        command.Parameters.AddWithValue("$total", Dinero.ACentavos(venta.Total));
        command.Parameters.AddWithValue("$metodoPago", venta.MetodoPago);
        command.Parameters.AddWithValue("$montoRecibido", Dinero.ACentavos(venta.MontoRecibido));
        command.Parameters.AddWithValue("$cambio", Dinero.ACentavos(venta.Cambio));
        command.Parameters.AddWithValue("$idCaja", (object?)venta.IdCaja ?? DBNull.Value);
        command.Parameters.AddWithValue("$descuento", Dinero.ACentavos(venta.Descuento));
        command.Parameters.AddWithValue("$idCliente", (object?)venta.IdCliente ?? DBNull.Value);
        command.Parameters.AddWithValue("$idUsuario", (object?)venta.IdUsuario ?? DBNull.Value);
        command.Parameters.AddWithValue("$idUsuarioDescuento", (object?)venta.IdUsuarioDescuento ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Inserta una línea. El costo unitario se toma del producto en el mismo
    /// instante (dentro de la transacción) para calcular la utilidad real.
    /// </summary>
    private static void InsertarDetalle(SqliteConnection connection, SqliteTransaction transaction, int idVenta, DetalleVenta detalle)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO DetalleVentas (IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal, Unidad, CostoUnitario)
            VALUES ($idVenta, $idProducto, $nombreProducto, $cantidad, $precioUnitario, $subtotal, $unidad,
                    (SELECT PrecioCompra FROM Productos WHERE IdProducto = $idProducto));
            """;
        command.Parameters.AddWithValue("$idVenta", idVenta);
        command.Parameters.AddWithValue("$idProducto", detalle.IdProducto);
        command.Parameters.AddWithValue("$nombreProducto", detalle.NombreProducto);
        command.Parameters.AddWithValue("$cantidad", Cantidades.AMilesimas(detalle.Cantidad));
        command.Parameters.AddWithValue("$precioUnitario", Dinero.ACentavos(detalle.PrecioUnitario));
        command.Parameters.AddWithValue("$subtotal", Dinero.ACentavos(detalle.Subtotal));
        command.Parameters.AddWithValue("$unidad", detalle.Unidad);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Anula una venta en una sola transacción:
    ///  1. Devuelve al inventario lo vendido.
    ///  2. Si la venta es de una caja anterior (ya cerrada) y se pagó en
    ///     efectivo, registra una salida en la caja abierta por la devolución
    ///     del dinero; si es de la caja abierta, simplemente deja de contar
    ///     en su arqueo.
    ///  3. Marca la venta como anulada con fecha, motivo, usuario y caja.
    /// Exige una caja abierta, para que toda anulación quede en una jornada.
    /// </summary>
    public void Anular(int idVenta, string motivo, int? idUsuario)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            string estado, metodoPago;
            int? idCajaVenta;
            decimal total;
            using (var consulta = connection.CreateCommand())
            {
                consulta.Transaction = transaction;
                consulta.CommandText = "SELECT Estado, MetodoPago, IdCaja, Total FROM Ventas WHERE IdVenta = $id;";
                consulta.Parameters.AddWithValue("$id", idVenta);
                using var reader = consulta.ExecuteReader();
                if (!reader.Read())
                {
                    throw new ReglaNegocioException($"La venta #{idVenta} no existe.");
                }

                estado = reader.GetString(0);
                metodoPago = reader.GetString(1);
                idCajaVenta = reader.IsDBNull(2) ? null : reader.GetInt32(2);
                total = reader.LeerDinero(3);
            }

            if (estado == Venta.EstadoAnulada)
            {
                throw new ReglaNegocioException($"La venta #{idVenta} ya está anulada.");
            }

            var idCajaAbierta = CajaRepository.ObtenerIdCajaAbierta(connection, transaction)
                ?? throw new ReglaNegocioException("Abra la caja antes de anular una venta: la anulación queda registrada en la jornada.");

            if (idCajaVenta != idCajaAbierta && metodoPago == Venta.PagoEfectivo && total > 0)
            {
                var totales = CajaRepository.ObtenerTotales(connection, transaction, idCajaAbierta)!;
                if (total > totales.SaldoEsperado)
                {
                    throw new ReglaNegocioException(
                        $"No hay efectivo suficiente en caja ({totales.SaldoEsperado:C2}) para devolver {total:C2}.");
                }

                CajaRepository.InsertarMovimiento(connection, transaction, new MovimientoCaja
                {
                    IdCaja = idCajaAbierta,
                    FechaHora = DateTime.Now,
                    Tipo = "Salida",
                    Monto = total,
                    Descripcion = $"Devolución por anulación de la venta #{idVenta}",
                    IdUsuario = idUsuario
                });
            }

            foreach (var detalle in LeerDetalle(connection, transaction, idVenta))
            {
                _productoRepository.AumentarExistencia(connection, transaction, detalle.IdProducto, detalle.Cantidad);
            }

            using (var anular = connection.CreateCommand())
            {
                anular.Transaction = transaction;
                anular.CommandText = """
                    UPDATE Ventas
                    SET Estado = 'Anulada', FechaAnulacion = $fecha, MotivoAnulacion = $motivo,
                        IdUsuarioAnulacion = $idUsuario, IdCajaAnulacion = $idCaja
                    WHERE IdVenta = $id AND Estado = 'Activa';
                    """;
                anular.Parameters.AddWithValue("$fecha", Fechas.ParaBaseDatos(DateTime.Now));
                anular.Parameters.AddWithValue("$motivo", motivo);
                anular.Parameters.AddWithValue("$idUsuario", (object?)idUsuario ?? DBNull.Value);
                anular.Parameters.AddWithValue("$idCaja", idCajaAbierta);
                anular.Parameters.AddWithValue("$id", idVenta);

                if (anular.ExecuteNonQuery() != 1)
                {
                    throw new ReglaNegocioException($"La venta #{idVenta} cambió mientras se anulaba. Intente de nuevo.");
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Venta? ObtenerPorId(int idVenta)
    {
        using var connection = DatabaseConnection.CreateConnection();

        Venta? venta;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"{ConsultaVenta} WHERE v.IdVenta = $id;";
            command.Parameters.AddWithValue("$id", idVenta);

            using var reader = command.ExecuteReader();
            venta = reader.Read() ? MapearVenta(reader) : null;
        }

        venta?.Detalles.AddRange(LeerDetalle(connection, null, idVenta));
        return venta;
    }

    private static List<DetalleVenta> LeerDetalle(SqliteConnection connection, SqliteTransaction? transaction, int idVenta)
    {
        var detalles = new List<DetalleVenta>();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT IdDetalleVenta, IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Unidad, CostoUnitario
            FROM DetalleVentas
            WHERE IdVenta = $id
            ORDER BY IdDetalleVenta;
            """;
        command.Parameters.AddWithValue("$id", idVenta);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            detalles.Add(new DetalleVenta
            {
                IdDetalleVenta = reader.GetInt32(0),
                IdVenta = reader.GetInt32(1),
                IdProducto = reader.GetInt32(2),
                NombreProducto = reader.GetString(3),
                Cantidad = reader.LeerCantidad(4),
                PrecioUnitario = reader.LeerDinero(5),
                Unidad = reader.GetString(6),
                CostoUnitario = reader.LeerDineroONulo(7)
            });
        }

        return detalles;
    }

    /// <summary>
    /// Ventas del rango de fechas (días completos), más recientes primero,
    /// o en orden cronológico si <paramref name="cronologico"/> es true.
    /// Usa el índice IX_Ventas_FechaHora: no recorre todo el historial.
    /// </summary>
    public List<Venta> ObtenerPorRango(DateTime? desde, DateTime? hasta, bool cronologico = false)
    {
        var orden = cronologico ? "v.FechaHora, v.IdVenta" : "v.FechaHora DESC, v.IdVenta DESC";

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            {ConsultaVenta}
            WHERE v.FechaHora BETWEEN $desde AND $hasta
            ORDER BY {orden};
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        var ventas = new List<Venta>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ventas.Add(MapearVenta(reader));
        }

        return ventas;
    }

    private static Venta MapearVenta(SqliteDataReader reader)
    {
        return new Venta
        {
            IdVenta = reader.GetInt32(0),
            FechaHora = Fechas.DesdeBaseDatos(reader.GetString(1)),
            Total = reader.LeerDinero(2),
            MetodoPago = reader.GetString(3),
            MontoRecibido = reader.LeerDineroONulo(4),
            Cambio = reader.LeerDineroONulo(5),
            IdCaja = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            Descuento = reader.LeerDinero(7),
            IdCliente = reader.IsDBNull(8) ? null : reader.GetInt32(8),
            NombreCliente = reader.IsDBNull(9) ? null : reader.GetString(9),
            IdUsuario = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            NombreUsuario = reader.IsDBNull(11) ? null : reader.GetString(11),
            Estado = reader.GetString(12),
            FechaAnulacion = reader.IsDBNull(13) ? null : Fechas.DesdeBaseDatos(reader.GetString(13)),
            MotivoAnulacion = reader.IsDBNull(14) ? null : reader.GetString(14),
            NombreUsuarioAnulacion = reader.IsDBNull(15) ? null : reader.GetString(15),
            IdCajaAnulacion = reader.IsDBNull(16) ? null : reader.GetInt32(16),
            IdUsuarioDescuento = reader.IsDBNull(17) ? null : reader.GetInt32(17)
        };
    }
}
