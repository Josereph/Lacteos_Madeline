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
    private const string ConsultaCaja = """
        SELECT c.IdCaja, c.FechaApertura, c.MontoInicial, c.FechaCierre, c.MontoFinal, c.Estado,
               c.MontoEsperado, c.Diferencia, c.IdUsuarioApertura, ua.Nombre, c.IdUsuarioCierre, uc.Nombre
        FROM Cajas c
        LEFT JOIN Usuarios ua ON ua.IdUsuario = c.IdUsuarioApertura
        LEFT JOIN Usuarios uc ON uc.IdUsuario = c.IdUsuarioCierre
        """;

    /// <summary>
    /// Una venta cuenta en el arqueo de su caja salvo que se haya anulado
    /// durante esa misma jornada. Si se anuló en una jornada posterior, la
    /// devolución del efectivo se registró como salida en esa otra caja.
    /// </summary>
    private const string VentaCuentaEnCaja = "NOT (v.Estado = 'Anulada' AND v.IdCajaAnulacion = c.IdCaja)";

    public int AbrirCaja(decimal montoInicial, int? idUsuario = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Cajas (FechaApertura, MontoInicial, Estado, IdUsuarioApertura)
            VALUES ($fechaApertura, $montoInicial, 'Abierta', $idUsuario);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fechaApertura", Fechas.ParaBaseDatos(DateTime.Now));
        command.Parameters.AddWithValue("$montoInicial", Dinero.ACentavos(montoInicial));
        command.Parameters.AddWithValue("$idUsuario", (object?)idUsuario ?? DBNull.Value);

        var id = Convert.ToInt32(command.ExecuteScalar());
        transaction.Commit();
        return id;
    }

    /// <summary>
    /// Cierra la caja de forma atómica: recalcula los totales, guarda el
    /// monto contado, el saldo esperado y la diferencia, todo en una sola
    /// transacción. Devuelve los totales usados para el arqueo.
    /// </summary>
    public TotalesCaja CerrarCaja(int idCaja, decimal montoFinal, int? idUsuario = null)
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
            SET FechaCierre     = $fechaCierre,
                MontoFinal      = $montoFinal,
                MontoEsperado   = $montoEsperado,
                Diferencia      = $diferencia,
                IdUsuarioCierre = $idUsuario,
                Estado          = 'Cerrada'
            WHERE IdCaja = $id AND Estado = 'Abierta';
            """;
        command.Parameters.AddWithValue("$fechaCierre", Fechas.ParaBaseDatos(DateTime.Now));
        command.Parameters.AddWithValue("$montoFinal", Dinero.ACentavos(montoFinal));
        command.Parameters.AddWithValue("$montoEsperado", Dinero.ACentavos(esperado));
        command.Parameters.AddWithValue("$diferencia", Dinero.ACentavos(montoFinal - esperado));
        command.Parameters.AddWithValue("$idUsuario", (object?)idUsuario ?? DBNull.Value);
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
        command.CommandText = $"{ConsultaCaja} WHERE c.Estado = 'Abierta' LIMIT 1;";

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    internal static int? ObtenerIdCajaAbierta(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT IdCaja FROM Cajas WHERE Estado = 'Abierta' LIMIT 1;";
        return command.ExecuteScalar() is long id ? (int)id : null;
    }

    public Caja? ObtenerPorId(int idCaja)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{ConsultaCaja} WHERE c.IdCaja = $id;";
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

        var id = InsertarMovimiento(connection, transaction, movimiento);
        transaction.Commit();
        return id;
    }

    internal static int InsertarMovimiento(SqliteConnection connection, SqliteTransaction transaction, MovimientoCaja movimiento)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO MovimientosCaja (IdCaja, FechaHora, Tipo, Monto, Descripcion, IdUsuario)
            VALUES ($idCaja, $fechaHora, $tipo, $monto, $descripcion, $idUsuario);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$idCaja", movimiento.IdCaja);
        command.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(movimiento.FechaHora));
        command.Parameters.AddWithValue("$tipo", movimiento.Tipo);
        command.Parameters.AddWithValue("$monto", Dinero.ACentavos(movimiento.Monto));
        command.Parameters.AddWithValue("$descripcion", (object?)movimiento.Descripcion ?? DBNull.Value);
        command.Parameters.AddWithValue("$idUsuario", (object?)movimiento.IdUsuario ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<MovimientoCaja> ObtenerMovimientos(int idCaja)
    {
        var movimientos = new List<MovimientoCaja>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.IdMovimiento, m.IdCaja, m.FechaHora, m.Tipo, m.Monto, m.Descripcion, m.IdUsuario, u.Nombre
            FROM MovimientosCaja m
            LEFT JOIN Usuarios u ON u.IdUsuario = m.IdUsuario
            WHERE m.IdCaja = $idCaja
            ORDER BY m.IdMovimiento;
            """;
        command.Parameters.AddWithValue("$idCaja", idCaja);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            movimientos.Add(new MovimientoCaja
            {
                IdMovimiento  = reader.GetInt32(0),
                IdCaja        = reader.GetInt32(1),
                FechaHora     = Fechas.DesdeBaseDatos(reader.GetString(2)),
                Tipo          = reader.GetString(3),
                Monto         = reader.LeerDinero(4),
                Descripcion   = reader.IsDBNull(5) ? null : reader.GetString(5),
                IdUsuario     = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                NombreUsuario = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return movimientos;
    }

    /// <summary>
    /// Totales de la caja en una sola consulta: fondo inicial, ventas en
    /// efectivo (lo único que entra físicamente a la gaveta), ventas con
    /// otros métodos, entradas y salidas, más conteos de ventas, anulaciones
    /// y descuentos. Usa los índices IX_Ventas_Caja_Metodo e
    /// IX_MovimientosCaja_Caja (sección 13 lineamientos).
    /// </summary>
    public TotalesCaja? ObtenerTotales(int idCaja)
    {
        using var connection = DatabaseConnection.CreateConnection();
        return ObtenerTotales(connection, null, idCaja);
    }

    internal static TotalesCaja? ObtenerTotales(SqliteConnection connection, SqliteTransaction? transaction, int idCaja)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT c.MontoInicial,
                   c.Estado,
                   (SELECT COALESCE(SUM(v.Total), 0) FROM Ventas v
                     WHERE v.IdCaja = c.IdCaja AND v.MetodoPago = 'Efectivo' AND {VentaCuentaEnCaja}),
                   (SELECT COALESCE(SUM(v.Total), 0) FROM Ventas v
                     WHERE v.IdCaja = c.IdCaja AND v.MetodoPago <> 'Efectivo' AND {VentaCuentaEnCaja}),
                   (SELECT COALESCE(SUM(m.Monto), 0) FROM MovimientosCaja m
                     WHERE m.IdCaja = c.IdCaja AND m.Tipo = 'Entrada'),
                   (SELECT COALESCE(SUM(m.Monto), 0) FROM MovimientosCaja m
                     WHERE m.IdCaja = c.IdCaja AND m.Tipo = 'Salida'),
                   (SELECT COUNT(*) FROM Ventas v WHERE v.IdCaja = c.IdCaja AND {VentaCuentaEnCaja}),
                   (SELECT COUNT(*) FROM Ventas v WHERE v.IdCajaAnulacion = c.IdCaja),
                   (SELECT COALESCE(SUM(v.Descuento), 0) FROM Ventas v WHERE v.IdCaja = c.IdCaja AND {VentaCuentaEnCaja})
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
            Salidas:            reader.LeerDinero(5))
        {
            NumeroVentas = reader.GetInt32(6),
            VentasAnuladas = reader.GetInt32(7),
            Descuentos = reader.LeerDinero(8)
        };
    }

    private static Caja Mapear(SqliteDataReader reader)
    {
        return new Caja
        {
            IdCaja                = reader.GetInt32(0),
            FechaApertura         = Fechas.DesdeBaseDatos(reader.GetString(1)),
            MontoInicial          = reader.LeerDinero(2),
            FechaCierre           = reader.IsDBNull(3) ? null : Fechas.DesdeBaseDatos(reader.GetString(3)),
            MontoFinal            = reader.LeerDineroONulo(4),
            Estado                = reader.GetString(5),
            MontoEsperado         = reader.LeerDineroONulo(6),
            Diferencia            = reader.LeerDineroONulo(7),
            IdUsuarioApertura     = reader.IsDBNull(8) ? null : reader.GetInt32(8),
            NombreUsuarioApertura = reader.IsDBNull(9) ? null : reader.GetString(9),
            IdUsuarioCierre       = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            NombreUsuarioCierre   = reader.IsDBNull(11) ? null : reader.GetString(11)
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
        command.CommandText = $"{ConsultaCaja} ORDER BY c.IdCaja DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cajas.Add(Mapear(reader));
        }

        return cajas;
    }
}
