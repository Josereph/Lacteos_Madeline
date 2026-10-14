using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data.Migraciones;

/// <summary>
/// v5: usuarios, clientes, anulación de ventas, descuentos y configuración.
///  - Usuarios: inicio de sesión con PIN (guardado como huella cifrada) y rol.
///  - Clientes: catálogo opcional asociado a cada venta.
///  - Configuracion: datos del negocio editables desde el programa.
///  - Ventas: descuento, cliente, usuario y anulación (estado, fecha, motivo,
///    quién anuló y en qué caja). Una venta solo puede cambiar para anularse.
///  - DetalleVentas.CostoUnitario: costo al momento de vender, para el
///    reporte de utilidad. Las ventas anteriores toman el costo actual.
///  - Quién abrió y cerró cada caja y quién registró movimientos, compras,
///    mermas y ajustes (los registros anteriores quedan sin usuario).
/// </summary>
internal static class V5UsuariosClientesAnulaciones
{
    public static void Aplicar(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE Usuarios (
                IdUsuario     INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre        TEXT NOT NULL COLLATE NOCASE UNIQUE,
                Rol           TEXT NOT NULL CHECK (Rol IN ('Administrador', 'Cajero')),
                PinHuella     TEXT NOT NULL,
                PinSal        TEXT NOT NULL,
                Estado        INTEGER NOT NULL DEFAULT 1,
                FechaCreacion TEXT NOT NULL
            );

            CREATE TABLE Clientes (
                IdCliente INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT NOT NULL,
                Documento TEXT NULL,
                Telefono  TEXT NULL,
                Direccion TEXT NULL,
                Correo    TEXT NULL,
                Estado    INTEGER NOT NULL DEFAULT 1
            );
            CREATE INDEX IX_Clientes_Estado_Nombre ON Clientes (Estado, Nombre);

            CREATE TABLE Configuracion (
                Clave TEXT PRIMARY KEY,
                Valor TEXT NOT NULL
            );

            ALTER TABLE Ventas ADD COLUMN Descuento INTEGER NOT NULL DEFAULT 0
                CHECK (typeof(Descuento) = 'integer' AND Descuento >= 0);
            ALTER TABLE Ventas ADD COLUMN IdCliente INTEGER NULL REFERENCES Clientes (IdCliente);
            ALTER TABLE Ventas ADD COLUMN IdUsuario INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE Ventas ADD COLUMN IdUsuarioDescuento INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE Ventas ADD COLUMN Estado TEXT NOT NULL DEFAULT 'Activa'
                CHECK (Estado IN ('Activa', 'Anulada'));
            ALTER TABLE Ventas ADD COLUMN FechaAnulacion TEXT NULL;
            ALTER TABLE Ventas ADD COLUMN MotivoAnulacion TEXT NULL;
            ALTER TABLE Ventas ADD COLUMN IdUsuarioAnulacion INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE Ventas ADD COLUMN IdCajaAnulacion INTEGER NULL REFERENCES Cajas (IdCaja);
            CREATE INDEX IX_Ventas_Cliente ON Ventas (IdCliente);

            ALTER TABLE DetalleVentas ADD COLUMN CostoUnitario INTEGER NULL
                CHECK (CostoUnitario IS NULL OR typeof(CostoUnitario) = 'integer');
            UPDATE DetalleVentas
            SET CostoUnitario = (SELECT p.PrecioCompra FROM Productos p WHERE p.IdProducto = DetalleVentas.IdProducto);

            ALTER TABLE Cajas ADD COLUMN IdUsuarioApertura INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE Cajas ADD COLUMN IdUsuarioCierre INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE MovimientosCaja ADD COLUMN IdUsuario INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE Compras ADD COLUMN IdUsuario INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE Mermas ADD COLUMN IdUsuario INTEGER NULL REFERENCES Usuarios (IdUsuario);
            ALTER TABLE AjustesInventario ADD COLUMN IdUsuario INTEGER NULL REFERENCES Usuarios (IdUsuario);

            -- Una venta registrada no se edita: solo puede anularse, una vez.
            CREATE TRIGGER TR_Ventas_SoloAnulacion
            BEFORE UPDATE ON Ventas
            WHEN OLD.Estado = 'Anulada'
              OR NEW.Estado <> 'Anulada'
              OR NEW.Total <> OLD.Total
              OR NEW.Descuento <> OLD.Descuento
              OR NEW.MetodoPago <> OLD.MetodoPago
              OR NEW.FechaHora <> OLD.FechaHora
              OR NEW.IdCaja IS NOT OLD.IdCaja
            BEGIN SELECT RAISE(ABORT, 'Una venta registrada no puede modificarse; solo puede anularse una vez.'); END;

            -- El detalle de una venta tampoco se edita ni se elimina.
            CREATE TRIGGER TR_DetalleVentas_Inmutable_Upd
            BEFORE UPDATE ON DetalleVentas
            BEGIN SELECT RAISE(ABORT, 'El detalle de una venta registrada no puede modificarse.'); END;

            CREATE TRIGGER TR_DetalleVentas_Inmutable_Del
            BEFORE DELETE ON DetalleVentas
            BEGIN SELECT RAISE(ABORT, 'El detalle de una venta registrada no puede eliminarse.'); END;
            """;
        command.ExecuteNonQuery();
    }
}
