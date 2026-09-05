using LacteosMadeline.Data;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para Cajas y MovimientosCaja.
/// Garantiza que solo exista una caja con Estado 'Abierta' al mismo tiempo
/// (validación de sección 23 de los lineamientos).
/// </summary>
public class CajaRepository
{
    public int AbrirCaja(decimal montoInicial)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Cajas (FechaApertura, MontoInicial, Estado)
            VALUES ($fechaApertura, $montoInicial, 'Abierta');
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaApertura", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$montoInicial", montoInicial);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void CerrarCaja(int idCaja, decimal montoFinal)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Cajas
            SET FechaCierre = $fechaCierre,
                MontoFinal  = $montoFinal,
                Estado      = 'Cerrada'
            WHERE IdCaja = $id;
            """;
        command.Parameters.AddWithValue("$fechaCierre", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$montoFinal", montoFinal);
        command.Parameters.AddWithValue("$id", idCaja);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Devuelve la caja actualmente abierta, o null si no hay ninguna.
    /// </summary>
    public Caja? ObtenerCajaAbierta()
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdCaja, FechaApertura, MontoInicial, FechaCierre, MontoFinal, Estado
            FROM Cajas
            WHERE Estado = 'Abierta'
            LIMIT 1;
            """;

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public Caja? ObtenerPorId(int idCaja)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdCaja, FechaApertura, MontoInicial, FechaCierre, MontoFinal, Estado
            FROM Cajas
            WHERE IdCaja = $id;
            """;
        command.Parameters.AddWithValue("$id", idCaja);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public int RegistrarMovimiento(MovimientoCaja movimiento)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO MovimientosCaja (IdCaja, FechaHora, Tipo, Monto, Descripcion)
            VALUES ($idCaja, $fechaHora, $tipo, $monto, $descripcion);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$idCaja", movimiento.IdCaja);
        command.Parameters.AddWithValue("$fechaHora", movimiento.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$tipo", movimiento.Tipo);
        command.Parameters.AddWithValue("$monto", movimiento.Monto);
        command.Parameters.AddWithValue("$descripcion", (object?)movimiento.Descripcion ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<MovimientoCaja> ObtenerMovimientos(int idCaja)
    {
        var movimientos = new List<MovimientoCaja>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdMovimiento, IdCaja, FechaHora, Tipo, Monto, Descripcion
            FROM MovimientosCaja
            WHERE IdCaja = $idCaja
            ORDER BY IdMovimiento;
            """;
        command.Parameters.AddWithValue("$idCaja", idCaja);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            movimientos.Add(new MovimientoCaja
            {
                IdMovimiento = reader.GetInt32(0),
                IdCaja       = reader.GetInt32(1),
                FechaHora    = DateTime.Parse(reader.GetString(2)),
                Tipo         = reader.GetString(3),
                Monto        = reader.GetDecimal(4),
                Descripcion  = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return movimientos;
    }

    /// <summary>
    /// Total de ventas registradas entre dos fechas, para el cálculo
    /// del saldo de caja (sección 13 lineamientos).
    /// </summary>
    public decimal ObtenerTotalVentas(DateTime desde, DateTime hasta)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(Total), 0)
            FROM Ventas
            WHERE FechaHora >= $desde AND FechaHora <= $hasta;
            """;
        command.Parameters.AddWithValue("$desde", desde.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$hasta", hasta.ToString("yyyy-MM-dd HH:mm:ss"));

        return Convert.ToDecimal(command.ExecuteScalar());
    }

    private static Caja Mapear(SqliteDataReader reader)
    {
        return new Caja
        {
            IdCaja        = reader.GetInt32(0),
            FechaApertura = DateTime.Parse(reader.GetString(1)),
            MontoInicial  = reader.GetDecimal(2),
            FechaCierre   = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
            MontoFinal    = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            Estado        = reader.GetString(5)
        };
    }

    /// <summary>
    /// Devuelve todas las cajas registradas ordenadas de la más reciente
    /// a la más antigua, para el historial en FormCaja.
    /// </summary>
    public List<Caja> ObtenerHistorial()
    {
        var cajas = new List<Caja>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdCaja, FechaApertura, MontoInicial, FechaCierre, MontoFinal, Estado
            FROM Cajas
            ORDER BY IdCaja DESC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cajas.Add(Mapear(reader));
        }

        return cajas;
    }
}

