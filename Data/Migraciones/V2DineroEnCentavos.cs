using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data.Migraciones;

/// <summary>
/// v2: los montos pasan de REAL (punto flotante) a centavos enteros
/// (INTEGER). Con REAL, 0.10 + 0.20 no es exactamente 0.30 y las sumas
/// de muchos registros acumulan residuos; con centavos las sumas son exactas.
///
/// SQLite no permite cambiar el tipo de una columna, así que cada tabla con
/// montos se reconstruye (crear nueva → copiar convirtiendo → eliminar la
/// vieja → renombrar). Antes se eliminan los triggers, porque algunos hacen
/// referencia a otras tablas y SQLite revalida el esquema en cada renombrado;
/// al final se vuelven a crear junto con los índices.
///
/// Las columnas de dinero llevan CHECK (typeof(...) = 'integer'): si por un
/// error de programación llegara un monto con decimales, la base lo rechaza.
/// </summary>
internal static class V2DineroEnCentavos
{
    public static void Aplicar(SqliteConnection connection, SqliteTransaction transaction)
    {
        Ejecutar(connection, transaction, """
            -- Conserva los contadores AUTOINCREMENT: un número de venta o de
            -- caja nunca debe reutilizarse, aunque se haya eliminado el registro.
            CREATE TEMP TABLE SecuenciasPrevias AS SELECT name, seq FROM sqlite_sequence;

            DROP TRIGGER IF EXISTS TR_Productos_ExistenciaNoNegativa_Upd;
            DROP TRIGGER IF EXISTS TR_Productos_ExistenciaNoNegativa_Ins;
            DROP TRIGGER IF EXISTS TR_DetalleVentas_Valido;
            DROP TRIGGER IF EXISTS TR_DetalleCompras_Valido;
            DROP TRIGGER IF EXISTS TR_Mermas_Valida;
            DROP TRIGGER IF EXISTS TR_Ventas_CajaAbierta;
            DROP TRIGGER IF EXISTS TR_MovimientosCaja_Valido;
            DROP TRIGGER IF EXISTS TR_Cajas_CerradaInmutable;

            -- ── Productos ───────────────────────────────────────────────
            CREATE TABLE Productos_v2 (
                IdProducto   INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre       TEXT NOT NULL,
                IdCategoria  INTEGER NOT NULL,
                PrecioCompra INTEGER NULL     CHECK (PrecioCompra IS NULL OR typeof(PrecioCompra) = 'integer'),
                PrecioVenta  INTEGER NOT NULL CHECK (typeof(PrecioVenta) = 'integer'),
                Existencia   INTEGER NOT NULL DEFAULT 0,
                StockMinimo  INTEGER NOT NULL DEFAULT 0,
                Estado       INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (IdCategoria) REFERENCES Categorias (IdCategoria)
            );
            INSERT INTO Productos_v2 (IdProducto, Nombre, IdCategoria, PrecioCompra, PrecioVenta, Existencia, StockMinimo, Estado)
            SELECT IdProducto, Nombre, IdCategoria,
                   CAST(ROUND(PrecioCompra * 100) AS INTEGER),
                   CAST(ROUND(PrecioVenta * 100) AS INTEGER),
                   Existencia, StockMinimo, Estado
            FROM Productos;
            DROP TABLE Productos;
            ALTER TABLE Productos_v2 RENAME TO Productos;

            -- ── Cajas ───────────────────────────────────────────────────
            CREATE TABLE Cajas_v2 (
                IdCaja        INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaApertura TEXT NOT NULL,
                MontoInicial  INTEGER NOT NULL CHECK (typeof(MontoInicial) = 'integer'),
                FechaCierre   TEXT NULL,
                MontoFinal    INTEGER NULL CHECK (MontoFinal IS NULL OR typeof(MontoFinal) = 'integer'),
                Estado        TEXT NOT NULL DEFAULT 'Abierta',
                MontoEsperado INTEGER NULL CHECK (MontoEsperado IS NULL OR typeof(MontoEsperado) = 'integer'),
                Diferencia    INTEGER NULL CHECK (Diferencia IS NULL OR typeof(Diferencia) = 'integer')
            );
            INSERT INTO Cajas_v2 (IdCaja, FechaApertura, MontoInicial, FechaCierre, MontoFinal, Estado, MontoEsperado, Diferencia)
            SELECT IdCaja, FechaApertura,
                   CAST(ROUND(MontoInicial * 100) AS INTEGER),
                   FechaCierre,
                   CAST(ROUND(MontoFinal * 100) AS INTEGER),
                   Estado,
                   CAST(ROUND(MontoEsperado * 100) AS INTEGER),
                   CAST(ROUND(Diferencia * 100) AS INTEGER)
            FROM Cajas;
            DROP TABLE Cajas;
            ALTER TABLE Cajas_v2 RENAME TO Cajas;

            -- ── Ventas ──────────────────────────────────────────────────
            CREATE TABLE Ventas_v2 (
                IdVenta       INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora     TEXT NOT NULL,
                Total         INTEGER NOT NULL CHECK (typeof(Total) = 'integer'),
                MetodoPago    TEXT NOT NULL DEFAULT 'Efectivo',
                MontoRecibido INTEGER NULL CHECK (MontoRecibido IS NULL OR typeof(MontoRecibido) = 'integer'),
                Cambio        INTEGER NULL CHECK (Cambio IS NULL OR typeof(Cambio) = 'integer'),
                IdCaja        INTEGER NULL REFERENCES Cajas (IdCaja)
            );
            INSERT INTO Ventas_v2 (IdVenta, FechaHora, Total, MetodoPago, MontoRecibido, Cambio, IdCaja)
            SELECT IdVenta, FechaHora,
                   CAST(ROUND(Total * 100) AS INTEGER),
                   MetodoPago,
                   CAST(ROUND(MontoRecibido * 100) AS INTEGER),
                   CAST(ROUND(Cambio * 100) AS INTEGER),
                   IdCaja
            FROM Ventas;
            DROP TABLE Ventas;
            ALTER TABLE Ventas_v2 RENAME TO Ventas;

            -- ── DetalleVentas ───────────────────────────────────────────
            CREATE TABLE DetalleVentas_v2 (
                IdDetalleVenta INTEGER PRIMARY KEY AUTOINCREMENT,
                IdVenta        INTEGER NOT NULL,
                IdProducto     INTEGER NOT NULL,
                NombreProducto TEXT NOT NULL,
                Cantidad       INTEGER NOT NULL,
                PrecioUnitario INTEGER NOT NULL CHECK (typeof(PrecioUnitario) = 'integer'),
                Subtotal       INTEGER NOT NULL CHECK (typeof(Subtotal) = 'integer'),
                FOREIGN KEY (IdVenta)    REFERENCES Ventas    (IdVenta),
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );
            INSERT INTO DetalleVentas_v2 (IdDetalleVenta, IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal)
            SELECT IdDetalleVenta, IdVenta, IdProducto, NombreProducto, Cantidad,
                   CAST(ROUND(PrecioUnitario * 100) AS INTEGER),
                   CAST(ROUND(Subtotal * 100) AS INTEGER)
            FROM DetalleVentas;
            DROP TABLE DetalleVentas;
            ALTER TABLE DetalleVentas_v2 RENAME TO DetalleVentas;

            -- ── Compras ─────────────────────────────────────────────────
            CREATE TABLE Compras_v2 (
                IdCompra    INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora   TEXT NOT NULL,
                IdProveedor INTEGER NOT NULL,
                Total       INTEGER NOT NULL CHECK (typeof(Total) = 'integer'),
                FOREIGN KEY (IdProveedor) REFERENCES Proveedores (IdProveedor)
            );
            INSERT INTO Compras_v2 (IdCompra, FechaHora, IdProveedor, Total)
            SELECT IdCompra, FechaHora, IdProveedor, CAST(ROUND(Total * 100) AS INTEGER)
            FROM Compras;
            DROP TABLE Compras;
            ALTER TABLE Compras_v2 RENAME TO Compras;

            -- ── DetalleCompras ──────────────────────────────────────────
            CREATE TABLE DetalleCompras_v2 (
                IdDetalleCompra INTEGER PRIMARY KEY AUTOINCREMENT,
                IdCompra        INTEGER NOT NULL,
                IdProducto      INTEGER NOT NULL,
                NombreProducto  TEXT NOT NULL,
                Cantidad        INTEGER NOT NULL,
                CostoUnitario   INTEGER NOT NULL CHECK (typeof(CostoUnitario) = 'integer'),
                Subtotal        INTEGER NOT NULL CHECK (typeof(Subtotal) = 'integer'),
                FOREIGN KEY (IdCompra)   REFERENCES Compras   (IdCompra),
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );
            INSERT INTO DetalleCompras_v2 (IdDetalleCompra, IdCompra, IdProducto, NombreProducto, Cantidad, CostoUnitario, Subtotal)
            SELECT IdDetalleCompra, IdCompra, IdProducto, NombreProducto, Cantidad,
                   CAST(ROUND(CostoUnitario * 100) AS INTEGER),
                   CAST(ROUND(Subtotal * 100) AS INTEGER)
            FROM DetalleCompras;
            DROP TABLE DetalleCompras;
            ALTER TABLE DetalleCompras_v2 RENAME TO DetalleCompras;

            -- ── MovimientosCaja ─────────────────────────────────────────
            CREATE TABLE MovimientosCaja_v2 (
                IdMovimiento INTEGER PRIMARY KEY AUTOINCREMENT,
                IdCaja       INTEGER NOT NULL,
                FechaHora    TEXT NOT NULL,
                Tipo         TEXT NOT NULL,
                Monto        INTEGER NOT NULL CHECK (typeof(Monto) = 'integer'),
                Descripcion  TEXT NULL,
                FOREIGN KEY (IdCaja) REFERENCES Cajas (IdCaja)
            );
            INSERT INTO MovimientosCaja_v2 (IdMovimiento, IdCaja, FechaHora, Tipo, Monto, Descripcion)
            SELECT IdMovimiento, IdCaja, FechaHora, Tipo, CAST(ROUND(Monto * 100) AS INTEGER), Descripcion
            FROM MovimientosCaja;
            DROP TABLE MovimientosCaja;
            ALTER TABLE MovimientosCaja_v2 RENAME TO MovimientosCaja;

            -- ── Mermas ──────────────────────────────────────────────────
            CREATE TABLE Mermas_v2 (
                IdMerma        INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora      TEXT NOT NULL,
                IdProducto     INTEGER NOT NULL,
                NombreProducto TEXT NOT NULL,
                Cantidad       INTEGER NOT NULL,
                Motivo         TEXT NOT NULL,
                CostoUnitario  INTEGER NULL CHECK (CostoUnitario IS NULL OR typeof(CostoUnitario) = 'integer'),
                Observacion    TEXT NULL,
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );
            INSERT INTO Mermas_v2 (IdMerma, FechaHora, IdProducto, NombreProducto, Cantidad, Motivo, CostoUnitario, Observacion)
            SELECT IdMerma, FechaHora, IdProducto, NombreProducto, Cantidad, Motivo,
                   CAST(ROUND(CostoUnitario * 100) AS INTEGER), Observacion
            FROM Mermas;
            DROP TABLE Mermas;
            ALTER TABLE Mermas_v2 RENAME TO Mermas;

            -- Restaura los contadores al valor más alto entre el previo y el actual.
            UPDATE sqlite_sequence
            SET seq = MAX(seq, COALESCE((SELECT p.seq FROM SecuenciasPrevias p WHERE p.name = sqlite_sequence.name), 0));
            INSERT INTO sqlite_sequence (name, seq)
            SELECT p.name, p.seq FROM SecuenciasPrevias p
            WHERE p.name NOT IN (SELECT name FROM sqlite_sequence)
              AND p.name IN (SELECT name FROM sqlite_master WHERE type = 'table');
            DROP TABLE SecuenciasPrevias;

            -- ── Índices (se eliminaron junto con las tablas) ────────────
            CREATE INDEX IX_Productos_Estado_Nombre ON Productos (Estado, Nombre);
            CREATE INDEX IX_Productos_Categoria     ON Productos (IdCategoria);
            CREATE INDEX IX_Ventas_FechaHora        ON Ventas (FechaHora);
            CREATE INDEX IX_Ventas_Caja_Metodo      ON Ventas (IdCaja, MetodoPago, Total);
            CREATE INDEX IX_DetalleVentas_Venta     ON DetalleVentas (IdVenta);
            CREATE INDEX IX_DetalleVentas_Producto  ON DetalleVentas (IdProducto);
            CREATE INDEX IX_Compras_FechaHora       ON Compras (FechaHora);
            CREATE INDEX IX_Compras_Proveedor       ON Compras (IdProveedor);
            CREATE INDEX IX_DetalleCompras_Compra   ON DetalleCompras (IdCompra);
            CREATE INDEX IX_MovimientosCaja_Caja    ON MovimientosCaja (IdCaja, Tipo, Monto);
            CREATE INDEX IX_Mermas_FechaHora        ON Mermas (FechaHora);
            CREATE INDEX IX_Mermas_Producto         ON Mermas (IdProducto);
            CREATE UNIQUE INDEX UX_Cajas_UnaAbierta ON Cajas (Estado) WHERE Estado = 'Abierta';

            -- ── Reglas de integridad (mismas que en v1) ─────────────────
            CREATE TRIGGER TR_Productos_ExistenciaNoNegativa_Upd
            BEFORE UPDATE OF Existencia ON Productos
            WHEN NEW.Existencia < 0
            BEGIN SELECT RAISE(ABORT, 'La existencia del producto no puede quedar negativa.'); END;

            CREATE TRIGGER TR_Productos_ExistenciaNoNegativa_Ins
            BEFORE INSERT ON Productos
            WHEN NEW.Existencia < 0 OR NEW.StockMinimo < 0 OR NEW.PrecioVenta < 0
            BEGIN SELECT RAISE(ABORT, 'Existencia, stock mínimo y precio no pueden ser negativos.'); END;

            CREATE TRIGGER TR_DetalleVentas_Valido
            BEFORE INSERT ON DetalleVentas
            WHEN NEW.Cantidad <= 0 OR NEW.PrecioUnitario < 0
            BEGIN SELECT RAISE(ABORT, 'Línea de venta inválida: cantidad o precio fuera de rango.'); END;

            CREATE TRIGGER TR_DetalleCompras_Valido
            BEFORE INSERT ON DetalleCompras
            WHEN NEW.Cantidad <= 0 OR NEW.CostoUnitario < 0
            BEGIN SELECT RAISE(ABORT, 'Línea de compra inválida: cantidad o costo fuera de rango.'); END;

            CREATE TRIGGER TR_Mermas_Valida
            BEFORE INSERT ON Mermas
            WHEN NEW.Cantidad <= 0
            BEGIN SELECT RAISE(ABORT, 'La cantidad de la merma debe ser mayor que cero.'); END;

            CREATE TRIGGER TR_Ventas_CajaAbierta
            BEFORE INSERT ON Ventas
            WHEN NEW.IdCaja IS NULL
              OR COALESCE((SELECT Estado FROM Cajas WHERE IdCaja = NEW.IdCaja), '') <> 'Abierta'
            BEGIN SELECT RAISE(ABORT, 'No hay una caja abierta para registrar la venta.'); END;

            CREATE TRIGGER TR_MovimientosCaja_Valido
            BEFORE INSERT ON MovimientosCaja
            WHEN NEW.Monto <= 0
              OR NEW.Tipo NOT IN ('Entrada', 'Salida')
              OR COALESCE((SELECT Estado FROM Cajas WHERE IdCaja = NEW.IdCaja), '') <> 'Abierta'
            BEGIN SELECT RAISE(ABORT, 'Movimiento de caja inválido o caja cerrada.'); END;

            CREATE TRIGGER TR_Cajas_CerradaInmutable
            BEFORE UPDATE ON Cajas
            WHEN OLD.Estado = 'Cerrada'
            BEGIN SELECT RAISE(ABORT, 'La caja ya está cerrada y no puede modificarse.'); END;
            """);
    }

    private static void Ejecutar(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
