using LacteosMadeline.Data;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using LacteosMadeline.Services;
using Microsoft.Data.Sqlite;

// DatabaseConnection es estático: las pruebas no pueden correr en paralelo
// porque cada una apunta la aplicación a su propia base temporal.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LacteosMadeline.Tests;

/// <summary>
/// Base de las pruebas de integración: cada prueba trabaja sobre una base
/// SQLite nueva en una carpeta temporal (con sus propios logs y respaldos),
/// que se elimina al terminar. Nunca toca la base de datos del negocio.
/// </summary>
public abstract class PruebaConBaseDatos : IDisposable
{
    protected const int IdCategoriaOtros = 1;

    protected string Carpeta { get; }

    protected PruebaConBaseDatos(bool inicializar = true)
    {
        Carpeta = Path.Combine(Path.GetTempPath(), "LacteosMadeline.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Carpeta);
        DatabaseConnection.ConfigurarRuta(Path.Combine(Carpeta, "LacteosMadeline.db"));
        Sesion.Cerrar();

        if (inicializar)
        {
            DatabaseInitializer.Inicializar();
            new ConfiguracionService().Cargar();
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(Carpeta, recursive: true);
        }
        catch (IOException)
        {
            // Un archivo retenido por el antivirus no debe hacer fallar la prueba.
        }

        GC.SuppressFinalize(this);
    }

    protected static int CrearProducto(string nombre, decimal precioVenta, decimal existencia, decimal? precioCompra = 1m,
        string unidad = UnidadMedida.Unidad, string? codigo = null)
    {
        var resultado = new ProductoService().Registrar(new Producto
        {
            Nombre = nombre,
            IdCategoria = IdCategoriaOtros,
            PrecioVenta = precioVenta,
            PrecioCompra = precioCompra,
            Existencia = existencia,
            StockMinimo = 0,
            Unidad = unidad,
            Codigo = codigo
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        return resultado.IdProducto;
    }

    protected static int CrearProveedor(string nombre = "Proveedor de prueba", bool activo = true)
    {
        return new ProveedorRepository().Registrar(new Proveedor { Nombre = nombre, Estado = activo });
    }

    /// <summary>Crea un usuario activo con el PIN 2468 y lo devuelve.</summary>
    protected static Usuario CrearUsuario(string nombre, string rol, string pin = "2468")
    {
        var resultado = new UsuarioService().Crear(nombre, rol, pin, pin);
        Assert.True(resultado.Exitoso, resultado.Mensaje);
        return new UsuarioRepository().ObtenerPorId(resultado.IdUsuario)!;
    }

    protected static Caja AbrirCaja(decimal montoInicial = 0m)
    {
        var resultado = new CajaService().AbrirCaja(montoInicial);
        Assert.True(resultado.Exitoso, resultado.Mensaje);
        return resultado.CajaAbierta!;
    }

    protected static decimal Existencia(int idProducto)
    {
        return new ProductoRepository().ObtenerPorId(idProducto)!.Existencia;
    }

    protected static long Contar(string tabla)
    {
        return Escalar<long>($"SELECT COUNT(*) FROM {tabla};");
    }

    protected static T Escalar<T>(string sql)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    protected static void Ejecutar(string sql)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Venta completa de un solo producto, en efectivo exacto.
    /// </summary>
    protected static (bool Exitoso, string Mensaje, Venta? VentaRegistrada) Vender(int idProducto, decimal cantidad)
    {
        var servicio = new VentaService();
        var agregado = servicio.AgregarProducto(idProducto, cantidad);
        if (!agregado.Exitoso)
        {
            return (false, agregado.Mensaje, null);
        }

        return servicio.ConfirmarVenta(Venta.PagoEfectivo, servicio.Total);
    }
}
