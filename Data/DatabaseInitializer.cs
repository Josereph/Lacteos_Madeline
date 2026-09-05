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

        CrearTablas(connection);
        SembrarDatosIniciales(connection);
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
