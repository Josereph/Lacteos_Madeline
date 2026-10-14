using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para Cajas y MovimientosCaja.
/// Garantiza que solo exista una caja con Estado 'Abierta' al mismo tiempo
/// (validación de sección 23 de los lineamientos; reforzado por el índice
/// único UX_Cajas_UnaAbierta).
/// Las escrituras que dependen de un cálculo (salidas y cierre) leen y
/// escriben dentro de la misma transacción: BeginTransaction() en
/// Microsoft.Data.Sqlite usa BEGIN IMMEDIATE (aislamiento Serializable),
/// por lo que nadie más puede escribir entre el cálculo y el guardado.
/// </summary>
public class CajaRepository
{
    private const string ColumnasCaja =
        "IdCaja, FechaApertura, MontoInicial, FechaCierre, MontoFinal, Estado, MontoEsperado, Diferencia";

    public int AbrirCaja(decimal montoInicial)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Cajas (FechaApertura, MontoInicial, Estado)
            VALUES ($fechaApertura, $montoInicial, 'Abierta');
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaApertura", Fechas.ParaBaseDatos(DateTime.Now));
        command.Parameters.AddWithValue("$montoInicial", Dinero.ACentavos(montoInicial));

        var id = Convert.ToInt32(command.ExecuteScalar());
        transaction.Commit();
        return id;
    }

    /// <summary>
    /// Cierra la caja de forma atómica: recalcula los totales, guarda el
    /// monto contado, el saldo esperado y la diferencia, todo en una sola
    /// transacción. Devuelve los totales usados para el arqueo.
    /// </summary>
    public TotalesCaja CerrarCaja(int idCaja, decimal montoFinal)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var totales = ObtenerTotales(connection, transaction, idCaja)
            ?? throw new ReglaNegocioException("No se encontró la caja indicada.");

        if (totales.Estado != "Abierta")
        {
            throw new ReglaNegocioException("La caja ya se encuentra cerrada.");
        }

        var esperado = totales.SaldoEsperado;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Cajas
            SET FechaCierre   = $fechaCierre,
                MontoFinal    = $montoFinal,
                MontoEsperado = $montoEsperado,
                Diferencia    = $diferencia,
                Estado        = 'Cerrada'
            WHERE IdCaja = $id AND Estado = 'Abierta';
            """;
        command.Parameters.AddWithValue("$fechaCierre", Fechas.ParaBaseDatos(DateTime.Now));
        command.Parameters.AddWithValue("$montoFinal", Dinero.ACentavos(montoFinal));
        command.Parameters.AddWithValue("$montoEsperado", Dinero.ACentavos(esperado));
        command.Parameters.AddWithValue("$diferencia", Dinero.ACentavos(montoFinal - esperado));
        command.Parameters.AddWithValue("$id", idCaja);

        if (command.ExecuteNonQuery() != 1)
        {
            throw new ReglaNegocioException("No se pudo cerrar la caja: su estado cambió.");
        }

        transaction.Commit();
        return totales;
    }

    /// <summary>
    /// Devuelve la caja actualmente abierta, o null si no hay ninguna.
    /// </summary>
    public Caja? ObtenerCajaAbierta()
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {ColumnasCaja} FROM Cajas WHERE Estado = 'Abierta' LIMIT 1;";

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public Caja? ObtenerPorId(int idCaja)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {ColumnasCaja} FROM Cajas WHERE IdCaja = $id;";
        command.Parameters.AddWithValue("$id", idCaja);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    /// <summary>
    /// Registra un movimiento manual. Para salidas verifica, dentro de la
    /// misma transacción, que no se retire más efectivo del que hay en caja.
    /// </summary>
    public int RegistrarMovimiento(MovimientoCaja movimiento)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var totales = ObtenerTotales(connection, transaction, movimiento.IdCaja)
            ?? throw new ReglaNegocioException("No se encontró la caja indicada.");

        if (totales.Estado != "Abierta")
        {
            throw new ReglaNegocioException("La caja está cerrada; no admite movimientos.");
        }

        if (movimiento.Tipo == "Salida" && movimiento.Monto > totales.SaldoEsperado)
        {
            throw new ReglaNegocioException(
                $"La salida ({movimiento.Monto:C2}) supera el efectivo disponible en caja ({totales.SaldoEsperado:C2}).");
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO MovimientosCaja (IdCaja, FechaHora, Tipo, Monto, Descripcion)
            VALUES ($idCaja, $fechaHora, $tipo, $monto, $descripcion);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$idCaja", movimiento.IdCaja);
        command.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(movimiento.FechaHora));
        command.Parameters.AddWithValue("$tipo", movimiento.Tipo);
        command.Parameters.AddWithValue("$monto", Dinero.ACentavos(movimiento.Monto));
        command.Parameters.AddWithValue("$descripcion", (object?)movimiento.Descripcion ?? DBNull.Value);

        var id = Convert.ToInt32(command.ExecuteScalar());
        transaction.Commit();
        return id;
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
                FechaHora    = Fechas.DesdeBaseDatos(reader.GetString(2)),
                Tipo         = reader.GetString(3),
                Monto        = reader.LeerDinero(4),
                Descripcion  = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return movimientos;
    }

    /// <summary>
    /// Totales de la caja en una sola consulta: fondo inicial, ventas en
    /// efectivo (lo único que entra físicamente a la gaveta), ventas con
    /// otros métodos, entradas y salidas. Usa los índices
    /// IX_Ventas_Caja_Metodo e IX_MovimientosCaja_Caja (sección 13 lineamientos).
    /// </summary>
    public TotalesCaja? ObtenerTotales(int idCaja)
    {
        using var connection = DatabaseConnection.CreateConnection();
        return ObtenerTotales(connection, null, idCaja);
    }

    private static TotalesCaja? ObtenerTotales(SqliteConnection connection, SqliteTransaction? transaction, int idCaja)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT c.MontoInicial,
                   c.Estado,
                   (SELECT COALESCE(SUM(v.Total), 0) FROM Ventas v
                     WHERE v.IdCaja = c.IdCaja AND v.MetodoPago = 'Efectivo'),
                   (SELECT COALESCE(SUM(v.Total), 0) FROM Ventas v
                     WHERE v.IdCaja = c.IdCaja AND v.MetodoPago <> 'Efectivo'),
                   (SELECT COALESCE(SUM(m.Monto), 0) FROM MovimientosCaja m
                     WHERE m.IdCaja = c.IdCaja AND m.Tipo = 'Entrada'),
                   (SELECT COALESCE(SUM(m.Monto), 0) FROM MovimientosCaja m
                     WHERE m.IdCaja = c.IdCaja AND m.Tipo = 'Salida')
            FROM Cajas c
            WHERE c.IdCaja = $idCaja;
            """;
        command.Parameters.AddWithValue("$idCaja", idCaja);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new TotalesCaja(
            MontoInicial:       reader.LeerDinero(0),
            Estado:             reader.GetString(1),
            VentasEfectivo:     reader.LeerDinero(2),
            VentasOtrosMetodos: reader.LeerDinero(3),
            Entradas:           reader.LeerDinero(4),
            Salidas:            reader.LeerDinero(5));
    }

    private static Caja Mapear(SqliteDataReader reader)
    {
        return new Caja
        {
            IdCaja        = reader.GetInt32(0),
            FechaApertura = Fechas.DesdeBaseDatos(reader.GetString(1)),
            MontoInicial  = reader.LeerDinero(2),
            FechaCierre   = reader.IsDBNull(3) ? null : Fechas.DesdeBaseDatos(reader.GetString(3)),
            MontoFinal    = reader.LeerDineroONulo(4),
            Estado        = reader.GetString(5),
            MontoEsperado = reader.LeerDineroONulo(6),
            Diferencia    = reader.LeerDineroONulo(7)
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
        command.CommandText = $"SELECT {ColumnasCaja} FROM Cajas ORDER BY IdCaja DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cajas.Add(Mapear(reader));
        }

        return cajas;
    }
}
