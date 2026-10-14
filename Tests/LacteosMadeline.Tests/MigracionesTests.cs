using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Tests;

/// <summary>
/// Versionado de la estructura (PRAGMA user_version): creación desde cero,
/// actualización de bases en uso sin pérdida de datos, respaldo previo,
/// reversión completa ante fallos y rechazo de bases más nuevas.
/// </summary>
public class MigracionesTests : PruebaConBaseDatos
{
    public MigracionesTests() : base(inicializar: false) { }

    private static int VersionActual() => Escalar<int>("PRAGMA user_version;");

    /// <summary>Número que tendría la próxima migración que se agregue.</summary>
    private static int Siguiente => DatabaseInitializer.VersionEsquema + 1;

    private string CarpetaRespaldosMigracion => Path.Combine(BackupHelper.CarpetaRespaldosAutomaticos, "Migraciones");

    private string[] RespaldosDeMigracion() =>
        Directory.Exists(CarpetaRespaldosMigracion) ? Directory.GetFiles(CarpetaRespaldosMigracion, "*.db") : [];

    private static bool ExisteTabla(string tabla) =>
        Escalar<long>($"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{tabla}';") > 0;

    [Fact]
    public void BaseNueva_QuedaEnLaVersionActualConTodaLaEstructura()
    {
        DatabaseInitializer.Inicializar();

        Assert.Equal(DatabaseInitializer.VersionEsquema, VersionActual());
        foreach (var tabla in new[] { "Categorias", "Productos", "Ventas", "DetalleVentas", "Proveedores",
                                      "Compras", "DetalleCompras", "Cajas", "MovimientosCaja", "Mermas",
                                      "AjustesInventario" })
        {
            Assert.True(ExisteTabla(tabla), $"Falta la tabla {tabla}");
        }

        Assert.Equal("Otros", Escalar<string>("SELECT Nombre FROM Categorias WHERE IdCategoria = 1;"));
        Assert.Equal("wal", Escalar<string>("PRAGMA journal_mode;"));
    }

    [Fact]
    public void BaseNueva_NoGeneraRespaldoPorqueNoHayDatosQueProteger()
    {
        DatabaseInitializer.Inicializar();

        Assert.Empty(RespaldosDeMigracion());
    }

    [Fact]
    public void InicializarDosVeces_NoRepiteMigracionesNiRespaldos()
    {
        DatabaseInitializer.Inicializar();
        CrearProducto("Queso fresco", 3.50m, 10);

        DatabaseInitializer.Inicializar();

        Assert.Equal(DatabaseInitializer.VersionEsquema, VersionActual());
        Assert.Equal(1, Contar("Productos"));
        Assert.Empty(RespaldosDeMigracion());
    }

    /// <summary>
    /// Una base creada antes del versionado (user_version = 0, Ventas sin
    /// método de pago, Cajas sin arqueo) se actualiza conservando los datos.
    /// </summary>
    [Fact]
    public void BaseAnteriorAlVersionado_SeActualizaSinPerderDatos()
    {
        CrearEsquemaAnteriorConDatos();

        DatabaseInitializer.Inicializar();

        Assert.Equal(DatabaseInitializer.VersionEsquema, VersionActual());
        Assert.Equal(7, Escalar<long>("SELECT Existencia FROM Productos WHERE IdProducto = 1;"));
        Assert.Equal(1, Contar("Ventas"));
        Assert.Equal(1, Contar("DetalleVentas"));
        Assert.Equal("Efectivo", Escalar<string>("SELECT MetodoPago FROM Ventas WHERE IdVenta = 1;"));
        Assert.Equal(1, Escalar<long>("SELECT IdCaja FROM Ventas WHERE IdVenta = 1;"));
        Assert.True(ExisteTabla("Mermas"));
        Assert.Equal(1, Escalar<long>("SELECT COUNT(*) FROM pragma_table_info('Cajas') WHERE name = 'MontoEsperado';"));
    }

    [Fact]
    public void BaseAnteriorAlVersionado_GeneraRespaldoVerificadoConLosDatosOriginales()
    {
        CrearEsquemaAnteriorConDatos();

        DatabaseInitializer.Inicializar();

        var respaldo = Assert.Single(RespaldosDeMigracion());
        Assert.Contains($"_v0-a-v{DatabaseInitializer.VersionEsquema}", Path.GetFileName(respaldo));

        using var connection = new SqliteConnection($"Data Source={respaldo};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM Ventas),
                   (SELECT COUNT(*) FROM pragma_table_info('Ventas') WHERE name = 'MetodoPago'),
                   (SELECT user_version FROM pragma_user_version);
            """;
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt64(0)); // contiene la venta
        Assert.Equal(0, reader.GetInt64(1)); // es la estructura ANTERIOR a migrar
        Assert.Equal(0, reader.GetInt64(2));
    }

    [Fact]
    public void MigracionQueFalla_SeRevierteCompletaYConservaLaVersionAnterior()
    {
        DatabaseInitializer.Inicializar();
        var idProducto = CrearProducto("Crema", 2.00m, 5);

        var migraciones = DatabaseInitializer.Migraciones.Append(new Migracion(Siguiente, "Migración defectuosa", (connection, transaction) =>
        {
            Sql(connection, transaction, "CREATE TABLE TablaNueva (Id INTEGER PRIMARY KEY);");
            Sql(connection, transaction, "UPDATE Productos SET Existencia = 999;");
            throw new InvalidOperationException("Fallo simulado a mitad de la migración");
        })).ToList();

        var error = Assert.Throws<ErrorMigracionException>(() => MigradorBaseDatos.Aplicar(migraciones));

        Assert.Contains("se revirtieron", error.Message);
        Assert.Equal(DatabaseInitializer.VersionEsquema, VersionActual());
        Assert.False(ExisteTabla("TablaNueva"));
        Assert.Equal(5, Existencia(idProducto));
        Assert.Single(RespaldosDeMigracion());
    }

    [Fact]
    public void MigracionQueRompeReferenciasEntreTablas_SeRechaza()
    {
        DatabaseInitializer.Inicializar();

        var migraciones = DatabaseInitializer.Migraciones.Append(new Migracion(Siguiente, "Deja un detalle huérfano", (connection, transaction) =>
            Sql(connection, transaction, """
                INSERT INTO DetalleVentas (IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal)
                VALUES (999, 999, 'Fantasma', 1, 1, 1);
                """))).ToList();

        var error = Assert.Throws<ErrorMigracionException>(() => MigradorBaseDatos.Aplicar(migraciones));

        Assert.Contains("referencia", error.Message);
        Assert.Equal(DatabaseInitializer.VersionEsquema, VersionActual());
        Assert.Equal(0, Contar("DetalleVentas"));
    }

    [Fact]
    public void VariasMigracionesPendientes_SeAplicanEnOrdenYSoloLasNuevas()
    {
        DatabaseInitializer.Inicializar();
        var ejecutadas = new List<int>();
        var primera = Siguiente;
        var segunda = Siguiente + 1;

        var migraciones = DatabaseInitializer.Migraciones
            .Append(new Migracion(primera, "Agrega columna", (c, t) => { ejecutadas.Add(primera); Sql(c, t, "ALTER TABLE Productos ADD COLUMN Prueba TEXT NULL;"); }))
            .Append(new Migracion(segunda, "Agrega tabla", (c, t) => { ejecutadas.Add(segunda); Sql(c, t, "CREATE TABLE Prueba (Id INTEGER PRIMARY KEY);"); }))
            .ToList();

        MigradorBaseDatos.Aplicar(migraciones);
        MigradorBaseDatos.Aplicar(migraciones);

        Assert.Equal([primera, segunda], ejecutadas);
        Assert.Equal(segunda, VersionActual());
        Assert.True(ExisteTabla("Prueba"));
        Assert.Contains($"_v{DatabaseInitializer.VersionEsquema}-a-v{segunda}", Path.GetFileName(Assert.Single(RespaldosDeMigracion())));
    }

    [Fact]
    public void BaseDeUnaVersionMasNueva_SeRechazaSinModificarla()
    {
        DatabaseInitializer.Inicializar();
        CrearProducto("Queso duro", 4.00m, 3);
        Ejecutar("PRAGMA user_version = 99;");

        var error = Assert.Throws<ErrorMigracionException>(DatabaseInitializer.Inicializar);

        Assert.Contains("más reciente", error.Message);
        Assert.Equal(99, VersionActual());
        Assert.Equal(1, Contar("Productos"));
    }

    [Fact]
    public void MigracionesConNumeracionIncorrecta_SeDetectanAntesDeTocarLaBase()
    {
        var migraciones = DatabaseInitializer.Migraciones
            .Append(new Migracion(Siguiente + 1, "Salta una versión", (_, _) => { }))
            .ToList();

        Assert.Throws<InvalidOperationException>(() => MigradorBaseDatos.Aplicar(migraciones));
    }

    /// <summary>
    /// v2: los montos REAL de una base en uso pasan a centavos enteros sin
    /// perder valor, y los contadores de Id no retroceden.
    /// </summary>
    [Fact]
    public void BaseAnteriorAlVersionado_ConvierteLosMontosACentavos()
    {
        CrearEsquemaAnteriorConDatos();
        Ejecutar("""
            INSERT INTO Ventas (FechaHora, Total) VALUES ('2026-01-05 11:00:00', 0.10);
            DELETE FROM Ventas WHERE IdVenta = 2;
            """);

        DatabaseInitializer.Inicializar();

        Assert.Equal(200, Escalar<long>("SELECT PrecioCompra FROM Productos WHERE IdProducto = 1;"));
        Assert.Equal(350, Escalar<long>("SELECT PrecioVenta FROM Productos WHERE IdProducto = 1;"));
        Assert.Equal(700, Escalar<long>("SELECT Total FROM Ventas WHERE IdVenta = 1;"));
        Assert.Equal(350, Escalar<long>("SELECT PrecioUnitario FROM DetalleVentas WHERE IdDetalleVenta = 1;"));
        Assert.Equal(2000, Escalar<long>("SELECT MontoInicial FROM Cajas WHERE IdCaja = 1;"));
        Assert.Equal(2700, Escalar<long>("SELECT MontoFinal FROM Cajas WHERE IdCaja = 1;"));
        Assert.Equal("integer", Escalar<string>("SELECT typeof(Total) FROM Ventas WHERE IdVenta = 1;"));

        // El Id 2 se usó y se eliminó: no debe volver a asignarse.
        Assert.Equal(2, Escalar<long>("SELECT seq FROM sqlite_sequence WHERE name = 'Ventas';"));

        // Las reglas de integridad y la caja única siguen activas tras reconstruir las tablas.
        Assert.Equal(8, Escalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger';"));
        Assert.Equal(1, Escalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'UX_Cajas_UnaAbierta';"));
        Assert.Equal(0, Escalar<long>("SELECT COUNT(*) FROM pragma_foreign_key_check;"));
        Assert.Equal("ok", Escalar<string>("PRAGMA integrity_check;"));
    }

    [Fact]
    public void MontoConDecimalesEnColumnaDeCentavos_SeRechaza()
    {
        DatabaseInitializer.Inicializar();

        var error = Assert.Throws<SqliteException>(() =>
            Ejecutar("INSERT INTO Productos (Nombre, IdCategoria, PrecioVenta) VALUES ('X', 1, 3.5);"));

        Assert.Equal(19, error.SqliteErrorCode);
    }

    private static void Sql(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Estructura que tenía el sistema antes de agregar método de pago,
    /// arqueo de caja, proveedores y mermas, con una jornada ya cerrada.
    /// </summary>
    private static void CrearEsquemaAnteriorConDatos()
    {
        Ejecutar("""
            CREATE TABLE Categorias (
                IdCategoria INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL UNIQUE,
                Estado INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE Productos (
                IdProducto INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                IdCategoria INTEGER NOT NULL,
                PrecioCompra REAL NULL,
                PrecioVenta REAL NOT NULL,
                Existencia INTEGER NOT NULL DEFAULT 0,
                StockMinimo INTEGER NOT NULL DEFAULT 0,
                Estado INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (IdCategoria) REFERENCES Categorias (IdCategoria));
            CREATE TABLE Ventas (
                IdVenta INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora TEXT NOT NULL,
                Total REAL NOT NULL);
            CREATE TABLE DetalleVentas (
                IdDetalleVenta INTEGER PRIMARY KEY AUTOINCREMENT,
                IdVenta INTEGER NOT NULL,
                IdProducto INTEGER NOT NULL,
                NombreProducto TEXT NOT NULL,
                Cantidad INTEGER NOT NULL,
                PrecioUnitario REAL NOT NULL,
                Subtotal REAL NOT NULL,
                FOREIGN KEY (IdVenta) REFERENCES Ventas (IdVenta),
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto));
            CREATE TABLE Cajas (
                IdCaja INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaApertura TEXT NOT NULL,
                MontoInicial REAL NOT NULL,
                FechaCierre TEXT NULL,
                MontoFinal REAL NULL,
                Estado TEXT NOT NULL DEFAULT 'Abierta');
            CREATE TABLE MovimientosCaja (
                IdMovimiento INTEGER PRIMARY KEY AUTOINCREMENT,
                IdCaja INTEGER NOT NULL,
                FechaHora TEXT NOT NULL,
                Tipo TEXT NOT NULL,
                Monto REAL NOT NULL,
                Descripcion TEXT NULL,
                FOREIGN KEY (IdCaja) REFERENCES Cajas (IdCaja));

            INSERT INTO Categorias (Nombre) VALUES ('Quesos');
            INSERT INTO Productos (Nombre, IdCategoria, PrecioCompra, PrecioVenta, Existencia)
                VALUES ('Queso fresco', 1, 2.00, 3.50, 7);
            INSERT INTO Cajas (FechaApertura, MontoInicial, FechaCierre, MontoFinal, Estado)
                VALUES ('2026-01-05 07:00:00', 20.00, '2026-01-05 18:00:00', 27.00, 'Cerrada');
            INSERT INTO Ventas (FechaHora, Total) VALUES ('2026-01-05 10:30:00', 7.00);
            INSERT INTO DetalleVentas (IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal)
                VALUES (1, 1, 'Queso fresco', 2, 3.50, 7.00);
            """);
    }
}
