using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data;

/// <summary>
/// Crea la estructura de la base de datos si todavía no existe y deja
/// información inicial mínima (la categoría "Otros"), de acuerdo con el
/// modelo de datos definido en la documentación del proyecto.
/// </summary>
public static class DatabaseInitializer
{
    public static void Inicializar()
    {
        using var connection = DatabaseConnection.CreateConnection();

        ActivarWal(connection);
        CrearTablas(connection);
        MigrarVentasMetodoPago(connection);
        MigrarCajasCierre(connection);
        CrearIndices(connection);
        CrearReglasIntegridad(connection);
        SembrarDatosIniciales(connection);
    }

    /// <summary>
    /// WAL (Write-Ahead Logging): las escrituras se agregan a un archivo
    /// aparte y se integran después. Un cierre abrupto nunca deja la base
    /// a medias, las lecturas no bloquean las escrituras y cada venta se
    /// confirma más rápido. Es persistente: basta fijarlo una vez.
    /// </summary>
    private static void ActivarWal(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Agrega a Cajas el saldo esperado y la diferencia calculados al
    /// momento del cierre, para que el arqueo quede registrado y no dependa
    /// de recalcularlo después.
    /// </summary>
    private static void MigrarCajasCierre(SqliteConnection connection)
    {
        if (ExisteColumna(connection, "Cajas", "MontoEsperado"))
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE Cajas ADD COLUMN MontoEsperado REAL NULL;
            ALTER TABLE Cajas ADD COLUMN Diferencia REAL NULL;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    /// <summary>
    /// Índices para las consultas frecuentes: búsquedas de productos,
    /// rangos de fechas en ventas/compras/mermas, totales por caja y
    /// detalle por venta/producto (reporte de más vendidos).
    /// </summary>
    private static void CrearIndices(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE INDEX IF NOT EXISTS IX_Productos_Estado_Nombre ON Productos (Estado, Nombre);
            CREATE INDEX IF NOT EXISTS IX_Productos_Categoria     ON Productos (IdCategoria);
            CREATE INDEX IF NOT EXISTS IX_Ventas_FechaHora        ON Ventas (FechaHora);
            CREATE INDEX IF NOT EXISTS IX_Ventas_Caja_Metodo      ON Ventas (IdCaja, MetodoPago, Total);
            CREATE INDEX IF NOT EXISTS IX_DetalleVentas_Venta     ON DetalleVentas (IdVenta);
            CREATE INDEX IF NOT EXISTS IX_DetalleVentas_Producto  ON DetalleVentas (IdProducto);
            CREATE INDEX IF NOT EXISTS IX_Compras_FechaHora       ON Compras (FechaHora);
            CREATE INDEX IF NOT EXISTS IX_Compras_Proveedor       ON Compras (IdProveedor);
            CREATE INDEX IF NOT EXISTS IX_DetalleCompras_Compra   ON DetalleCompras (IdCompra);
            CREATE INDEX IF NOT EXISTS IX_MovimientosCaja_Caja    ON MovimientosCaja (IdCaja, Tipo, Monto);
            CREATE INDEX IF NOT EXISTS IX_Mermas_FechaHora        ON Mermas (FechaHora);
            CREATE INDEX IF NOT EXISTS IX_Mermas_Producto         ON Mermas (IdProducto);

            -- Solo puede existir UNA caja abierta: lo garantiza la propia base.
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Cajas_UnaAbierta ON Cajas (Estado) WHERE Estado = 'Abierta';
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Reglas de integridad aplicadas por la propia base de datos, como
    /// última línea de defensa aunque un error de programación saltara las
    /// validaciones de los servicios. Si se violan, la operación completa
    /// se revierte (RAISE ABORT dentro de la transacción).
    /// </summary>
    private static void CrearReglasIntegridad(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TRIGGER IF NOT EXISTS TR_Productos_ExistenciaNoNegativa_Upd
            BEFORE UPDATE OF Existencia ON Productos
            WHEN NEW.Existencia < 0
            BEGIN SELECT RAISE(ABORT, 'La existencia del producto no puede quedar negativa.'); END;

            CREATE TRIGGER IF NOT EXISTS TR_Productos_ExistenciaNoNegativa_Ins
            BEFORE INSERT ON Productos
            WHEN NEW.Existencia < 0 OR NEW.StockMinimo < 0 OR NEW.PrecioVenta < 0
            BEGIN SELECT RAISE(ABORT, 'Existencia, stock mínimo y precio no pueden ser negativos.'); END;

            CREATE TRIGGER IF NOT EXISTS TR_DetalleVentas_Valido
            BEFORE INSERT ON DetalleVentas
            WHEN NEW.Cantidad <= 0 OR NEW.PrecioUnitario < 0
            BEGIN SELECT RAISE(ABORT, 'Línea de venta inválida: cantidad o precio fuera de rango.'); END;

            CREATE TRIGGER IF NOT EXISTS TR_DetalleCompras_Valido
            BEFORE INSERT ON DetalleCompras
            WHEN NEW.Cantidad <= 0 OR NEW.CostoUnitario < 0
            BEGIN SELECT RAISE(ABORT, 'Línea de compra inválida: cantidad o costo fuera de rango.'); END;

            CREATE TRIGGER IF NOT EXISTS TR_Mermas_Valida
            BEFORE INSERT ON Mermas
            WHEN NEW.Cantidad <= 0
            BEGIN SELECT RAISE(ABORT, 'La cantidad de la merma debe ser mayor que cero.'); END;

            CREATE TRIGGER IF NOT EXISTS TR_Ventas_CajaAbierta
            BEFORE INSERT ON Ventas
            WHEN NEW.IdCaja IS NULL
              OR COALESCE((SELECT Estado FROM Cajas WHERE IdCaja = NEW.IdCaja), '') <> 'Abierta'
            BEGIN SELECT RAISE(ABORT, 'No hay una caja abierta para registrar la venta.'); END;

            CREATE TRIGGER IF NOT EXISTS TR_MovimientosCaja_Valido
            BEFORE INSERT ON MovimientosCaja
            WHEN NEW.Monto <= 0
              OR NEW.Tipo NOT IN ('Entrada', 'Salida')
              OR COALESCE((SELECT Estado FROM Cajas WHERE IdCaja = NEW.IdCaja), '') <> 'Abierta'
            BEGIN SELECT RAISE(ABORT, 'Movimiento de caja inválido o caja cerrada.'); END;

            -- Una caja cerrada es un registro contable: no se modifica.
            CREATE TRIGGER IF NOT EXISTS TR_Cajas_CerradaInmutable
            BEFORE UPDATE ON Cajas
            WHEN OLD.Estado = 'Cerrada'
            BEGIN SELECT RAISE(ABORT, 'La caja ya está cerrada y no puede modificarse.'); END;
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Agrega a Ventas las columnas de método de pago, monto recibido,
    /// cambio y caja, en bases de datos creadas antes de este cambio.
    /// Las ventas anteriores se marcan como "Efectivo" (así se contaban
    /// en caja) y se asocian a la caja cuya jornada las contenía.
    /// </summary>
    private static void MigrarVentasMetodoPago(SqliteConnection connection)
    {
        if (ExisteColumna(connection, "Ventas", "MetodoPago"))
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE Ventas ADD COLUMN MetodoPago TEXT NOT NULL DEFAULT 'Efectivo';
            ALTER TABLE Ventas ADD COLUMN MontoRecibido REAL NULL;
            ALTER TABLE Ventas ADD COLUMN Cambio REAL NULL;
            ALTER TABLE Ventas ADD COLUMN IdCaja INTEGER NULL REFERENCES Cajas (IdCaja);

            UPDATE Ventas
            SET IdCaja = (
                SELECT c.IdCaja FROM Cajas c
                WHERE Ventas.FechaHora >= c.FechaApertura
                  AND (c.FechaCierre IS NULL OR Ventas.FechaHora <= c.FechaCierre)
                ORDER BY c.IdCaja DESC
                LIMIT 1);
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static bool ExisteColumna(SqliteConnection connection, string tabla, string columna)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tabla}') WHERE name = $columna;";
        command.Parameters.AddWithValue("$columna", columna);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void CrearTablas(SqliteConnection connection)
    {
        const string script = """
            CREATE TABLE IF NOT EXISTS Categorias (
                IdCategoria INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL UNIQUE,
                Estado INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Productos (
                IdProducto INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                IdCategoria INTEGER NOT NULL,
                PrecioCompra REAL NULL,
                PrecioVenta REAL NOT NULL,
                Existencia INTEGER NOT NULL DEFAULT 0,
                StockMinimo INTEGER NOT NULL DEFAULT 0,
                Estado INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (IdCategoria) REFERENCES Categorias (IdCategoria)
            );

            CREATE TABLE IF NOT EXISTS Ventas (
                IdVenta INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora TEXT NOT NULL,
                Total REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS DetalleVentas (
                IdDetalleVenta INTEGER PRIMARY KEY AUTOINCREMENT,
                IdVenta INTEGER NOT NULL,
                IdProducto INTEGER NOT NULL,
                NombreProducto TEXT NOT NULL,
                Cantidad INTEGER NOT NULL,
                PrecioUnitario REAL NOT NULL,
                Subtotal REAL NOT NULL,
                FOREIGN KEY (IdVenta) REFERENCES Ventas (IdVenta),
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );

            CREATE TABLE IF NOT EXISTS Proveedores (
                IdProveedor   INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre        TEXT NOT NULL,
                Contacto      TEXT NULL,
                Telefono      TEXT NULL,
                Direccion     TEXT NULL,
                Observaciones TEXT NULL,
                Estado        INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Compras (
                IdCompra    INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora   TEXT NOT NULL,
                IdProveedor INTEGER NOT NULL,
                Total       REAL NOT NULL,
                FOREIGN KEY (IdProveedor) REFERENCES Proveedores (IdProveedor)
            );

            CREATE TABLE IF NOT EXISTS DetalleCompras (
                IdDetalleCompra INTEGER PRIMARY KEY AUTOINCREMENT,
                IdCompra        INTEGER NOT NULL,
                IdProducto      INTEGER NOT NULL,
                NombreProducto  TEXT NOT NULL,
                Cantidad        INTEGER NOT NULL,
                CostoUnitario   REAL NOT NULL,
                Subtotal        REAL NOT NULL,
                FOREIGN KEY (IdCompra)   REFERENCES Compras   (IdCompra),
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );

            CREATE TABLE IF NOT EXISTS Cajas (
                IdCaja        INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaApertura TEXT NOT NULL,
                MontoInicial  REAL NOT NULL,
                FechaCierre   TEXT NULL,
                MontoFinal    REAL NULL,
                Estado        TEXT NOT NULL DEFAULT 'Abierta'
            );

            CREATE TABLE IF NOT EXISTS MovimientosCaja (
                IdMovimiento INTEGER PRIMARY KEY AUTOINCREMENT,
                IdCaja       INTEGER NOT NULL,
                FechaHora    TEXT NOT NULL,
                Tipo         TEXT NOT NULL,
                Monto        REAL NOT NULL,
                Descripcion  TEXT NULL,
                FOREIGN KEY (IdCaja) REFERENCES Cajas (IdCaja)
            );

            -- Bajas de inventario por caducidad, daño u otro motivo.
            -- No afectan ventas ni caja; conservan nombre y costo históricos.
            CREATE TABLE IF NOT EXISTS Mermas (
                IdMerma        INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora      TEXT NOT NULL,
                IdProducto     INTEGER NOT NULL,
                NombreProducto TEXT NOT NULL,
                Cantidad       INTEGER NOT NULL,
                Motivo         TEXT NOT NULL,
                CostoUnitario  REAL NULL,
                Observacion    TEXT NULL,
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );
            """;

        using var command = connection.CreateCommand();
        command.CommandText = script;
        command.ExecuteNonQuery();
    }

    private static void SembrarDatosIniciales(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Categorias (Nombre, Estado) VALUES ('Otros', 1);";
        command.ExecuteNonQuery();
    }
}
