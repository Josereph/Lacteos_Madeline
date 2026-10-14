using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data.Migraciones;

/// <summary>
/// v4: venta por peso y códigos de producto.
///  - Productos.Unidad (Unidad, Libra, Kilogramo, Litro) y Productos.Codigo
///    (código de barras o interno, único cuando se indica).
///  - Las cantidades pasan a milésimas enteras para admitir hasta 3
///    decimales (1.250 lb = 1250) con sumas exactas. Las columnas ya eran
///    INTEGER, por lo que basta multiplicar los valores existentes por 1000.
///  - DetalleVentas.Unidad conserva la unidad con que se vendió cada línea.
/// </summary>
internal static class V4UnidadesYCodigos
{
    public static void Aplicar(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE Productos ADD COLUMN Unidad TEXT NOT NULL DEFAULT 'Unidad';
            ALTER TABLE Productos ADD COLUMN Codigo TEXT NULL;
            CREATE UNIQUE INDEX UX_Productos_Codigo ON Productos (Codigo) WHERE Codigo IS NOT NULL;

            ALTER TABLE DetalleVentas ADD COLUMN Unidad TEXT NOT NULL DEFAULT 'Unidad';

            UPDATE Productos         SET Existencia = Existencia * 1000, StockMinimo = StockMinimo * 1000;
            UPDATE DetalleVentas     SET Cantidad = Cantidad * 1000;
            UPDATE DetalleCompras    SET Cantidad = Cantidad * 1000;
            UPDATE Mermas            SET Cantidad = Cantidad * 1000;
            UPDATE AjustesInventario SET ExistenciaAnterior = ExistenciaAnterior * 1000,
                                         ExistenciaNueva    = ExistenciaNueva * 1000;
            """;
        command.ExecuteNonQuery();
    }
}
