using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Tests;

/// <summary>
/// Un respaldo solo sirve si puede abrirse y contiene los datos: se
/// comprueba abriéndolo y usándolo como base activa.
/// </summary>
public class RespaldoTests : PruebaConBaseDatos
{
    [Fact]
    public void Respaldo_ContieneLosDatosYPuedeUsarseComoBaseActiva()
    {
        AbrirCaja(15m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10);
        Assert.True(Vender(idProducto, 4).Exitoso);
        var destino = Path.Combine(Carpeta, "externo", "copia.db");

        var resultado = BackupHelper.RealizarBackup(destino);
        Assert.True(resultado.Exitoso, resultado.Detalle);

        // Simula la restauración: el programa pasa a trabajar sobre la copia.
        DatabaseConnection.ConfigurarRuta(destino);
        DatabaseInitializer.Inicializar();

        Assert.Equal(DatabaseInitializer.VersionEsquema, Escalar<int>("PRAGMA user_version;"));
        Assert.Equal(6, Existencia(idProducto));
        Assert.Equal(1, Contar("Ventas"));
        Assert.Equal("ok", Escalar<string>("PRAGMA integrity_check;"));
        Assert.True(Vender(idProducto, 1).Exitoso); // sigue operando: la caja abierta viajó en la copia
    }

    [Fact]
    public void RespaldoSobreLaBaseActiva_SeRechaza()
    {
        var resultado = BackupHelper.RealizarBackup(DatabaseConnection.DatabasePath);

        Assert.False(resultado.Exitoso);
        Assert.True(File.Exists(DatabaseConnection.DatabasePath));
    }

    [Fact]
    public void RespaldoSobreUnArchivoExistente_LoReemplaza()
    {
        var destino = Path.Combine(Carpeta, "copia.db");
        File.WriteAllText(destino, "contenido viejo que no es una base de datos");

        Assert.True(BackupHelper.RealizarBackup(destino).Exitoso);

        using var connection = new SqliteConnection($"Data Source={destino};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Categorias;";
        Assert.Equal(1L, command.ExecuteScalar());
    }
}
