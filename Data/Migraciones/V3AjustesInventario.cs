using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Data.Migraciones;

/// <summary>
/// v3: registro de ajustes manuales de inventario (conteo físico o
/// corrección desde la pantalla de productos). Antes el ajuste solo quedaba
/// en logs.txt, que rota y se pierde; ahora cada ajuste guarda, en la misma
/// transacción que cambia la existencia, el valor anterior, el nuevo y el motivo.
/// </summary>
internal static class V3AjustesInventario
{
    public static void Aplicar(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE AjustesInventario (
                IdAjuste           INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora          TEXT NOT NULL,
                IdProducto         INTEGER NOT NULL,
                NombreProducto     TEXT NOT NULL,
                ExistenciaAnterior INTEGER NOT NULL,
                ExistenciaNueva    INTEGER NOT NULL CHECK (ExistenciaNueva >= 0),
                Motivo             TEXT NOT NULL,
                FOREIGN KEY (IdProducto) REFERENCES Productos (IdProducto)
            );

            CREATE INDEX IX_AjustesInventario_FechaHora ON AjustesInventario (FechaHora);
            CREATE INDEX IX_AjustesInventario_Producto  ON AjustesInventario (IdProducto);
            """;
        command.ExecuteNonQuery();
    }
}
