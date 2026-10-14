using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para Mermas (bajas por caducidad o daño). El descuento
/// de inventario y el registro de la merma ocurren en una misma
/// transacción: o se hacen ambos, o ninguno. No toca Ventas ni Cajas.
/// </summary>
public class MermaRepository
{
    private readonly ProductoRepository _productoRepository = new();

    public int Registrar(Merma merma)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Lanza ReglaNegocioException si no hay existencia suficiente.
            _productoRepository.DescontarExistencia(connection, transaction, merma.IdProducto, merma.Cantidad);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Mermas (FechaHora, IdProducto, NombreProducto, Cantidad, Motivo, CostoUnitario, Observacion)
                VALUES ($fechaHora, $idProducto, $nombreProducto, $cantidad, $motivo, $costoUnitario, $observacion);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(merma.FechaHora));
            command.Parameters.AddWithValue("$idProducto", merma.IdProducto);
            command.Parameters.AddWithValue("$nombreProducto", merma.NombreProducto);
            command.Parameters.AddWithValue("$cantidad", merma.Cantidad);
            command.Parameters.AddWithValue("$motivo", merma.Motivo);
            command.Parameters.AddWithValue("$costoUnitario", Dinero.ACentavos(merma.CostoUnitario));
            command.Parameters.AddWithValue("$observacion", (object?)merma.Observacion ?? DBNull.Value);

            var idMerma = Convert.ToInt32(command.ExecuteScalar());
            transaction.Commit();
            return idMerma;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Mermas del rango de fechas, más recientes primero (índice IX_Mermas_FechaHora).
    /// </summary>
    public List<Merma> ObtenerPorRango(DateTime? desde, DateTime? hasta)
    {
        var mermas = new List<Merma>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdMerma, FechaHora, IdProducto, NombreProducto, Cantidad, Motivo, CostoUnitario, Observacion
            FROM Mermas
            WHERE FechaHora BETWEEN $desde AND $hasta
            ORDER BY FechaHora DESC, IdMerma DESC;
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            mermas.Add(Mapear(reader));
        }

        return mermas;
    }

    private static Merma Mapear(SqliteDataReader reader)
    {
        return new Merma
        {
            IdMerma        = reader.GetInt32(0),
            FechaHora      = Fechas.DesdeBaseDatos(reader.GetString(1)),
            IdProducto     = reader.GetInt32(2),
            NombreProducto = reader.GetString(3),
            Cantidad       = reader.GetInt32(4),
            Motivo         = reader.GetString(5),
            CostoUnitario  = reader.LeerDineroONulo(6),
            Observacion    = reader.IsDBNull(7) ? null : reader.GetString(7)
        };
    }
}
