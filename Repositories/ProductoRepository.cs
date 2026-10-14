using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para la tabla Productos. Las consultas incluyen el
/// nombre de la categoría mediante un JOIN para facilitar su uso directo
/// en la interfaz.
///
/// La existencia solo cambia por movimientos con registro propio: ventas,
/// compras, mermas o un ajuste (<see cref="AjustarExistencia"/>). Modificar
/// los datos del producto nunca la toca.
/// </summary>
public class ProductoRepository
{
    private const string ColumnasProducto = """
        p.IdProducto, p.Nombre, p.IdCategoria, c.Nombre, p.PrecioCompra,
        p.PrecioVenta, p.Existencia, p.StockMinimo, p.Estado
        """;

    /// <summary>
    /// Registra un producto nuevo. Su existencia inicial se guarda tal cual.
    /// </summary>
    public int Registrar(Producto producto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Productos (Nombre, IdCategoria, PrecioCompra, PrecioVenta, Existencia, StockMinimo, Estado)
            VALUES ($nombre, $idCategoria, $precioCompra, $precioVenta, $existencia, $stockMinimo, $estado);
            SELECT last_insert_rowid();
            """;
        AgregarParametros(command, producto);
        command.Parameters.AddWithValue("$existencia", producto.Existencia);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Actualiza nombre, categoría, precios, stock mínimo y estado. La
    /// existencia NO se modifica aquí: si el formulario tenía un valor viejo
    /// (por ejemplo, se vendió mientras se editaba), sobrescribirla borraría
    /// esos movimientos. Para corregirla use <see cref="AjustarExistencia"/>.
    /// </summary>
    public void Modificar(Producto producto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Productos
            SET Nombre = $nombre,
                IdCategoria = $idCategoria,
                PrecioCompra = $precioCompra,
                PrecioVenta = $precioVenta,
                StockMinimo = $stockMinimo,
                Estado = $estado
            WHERE IdProducto = $id;
            """;
        AgregarParametros(command, producto);
        command.Parameters.AddWithValue("$id", producto.IdProducto);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new ReglaNegocioException("El producto ya no existe.");
        }
    }

    public void CambiarEstado(int idProducto, bool estado)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Productos SET Estado = $estado WHERE IdProducto = $id;";
        command.Parameters.AddWithValue("$estado", estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", idProducto);
        command.ExecuteNonQuery();
    }

    public List<Producto> Buscar(string filtro = "", bool soloActivos = false)
    {
        var productos = new List<Producto>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnasProducto}
            FROM Productos p
            INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria
            WHERE p.Nombre LIKE $filtro
              AND (($soloActivos = 0) OR (p.Estado = 1))
            ORDER BY p.Nombre;
            """;
        command.Parameters.AddWithValue("$filtro", $"%{filtro}%");
        command.Parameters.AddWithValue("$soloActivos", soloActivos ? 1 : 0);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(Mapear(reader));
        }

        return productos;
    }

    public Producto? ObtenerPorId(int idProducto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnasProducto}
            FROM Productos p
            INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria
            WHERE p.IdProducto = $id;
            """;
        command.Parameters.AddWithValue("$id", idProducto);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    /// <summary>
    /// Descuenta existencia dentro de una conexión y transacción ya abiertas.
    /// Se utiliza al confirmar una venta para que el descuento de inventario
    /// y el registro de la venta ocurran de forma atómica (todo o nada).
    /// La condición "Existencia >= $cantidad" en el propio UPDATE evita
    /// vender más unidades de las disponibles (regla RN-05).
    /// </summary>
    public void DescontarExistencia(SqliteConnection connection, SqliteTransaction transaction, int idProducto, int cantidad)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Productos
            SET Existencia = Existencia - $cantidad
            WHERE IdProducto = $id AND Existencia >= $cantidad;
            """;
        command.Parameters.AddWithValue("$cantidad", cantidad);
        command.Parameters.AddWithValue("$id", idProducto);

        var filasAfectadas = command.ExecuteNonQuery();
        if (filasAfectadas == 0)
        {
            throw new ReglaNegocioException("Existencia insuficiente.");
        }
    }

    /// <summary>
    /// Aumenta existencia dentro de una transacción ya abierta (compras).
    /// Falla si el producto no existe, para que la compra completa se revierta.
    /// </summary>
    public void AumentarExistencia(SqliteConnection connection, SqliteTransaction transaction, int idProducto, int cantidad)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE Productos SET Existencia = Existencia + $cantidad WHERE IdProducto = $id;";
        command.Parameters.AddWithValue("$cantidad", cantidad);
        command.Parameters.AddWithValue("$id", idProducto);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new ReglaNegocioException($"El producto #{idProducto} ya no existe.");
        }
    }

    /// <summary>
    /// Ajuste manual de existencia (conteo físico o corrección). Lee el valor
    /// actual, lo cambia y registra el ajuste en AjustesInventario, todo en
    /// una sola transacción (BEGIN IMMEDIATE: nadie más escribe en medio).
    /// Si se indica <paramref name="existenciaEsperada"/> y la existencia
    /// real ya es otra (hubo una venta o compra mientras se editaba), el
    /// ajuste se rechaza para no borrar ese movimiento.
    /// Devuelve la existencia anterior.
    /// </summary>
    public int AjustarExistencia(int idProducto, int nuevaExistencia, string motivo, int? existenciaEsperada = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        string nombre;
        int anterior;
        using (var consulta = connection.CreateCommand())
        {
            consulta.Transaction = transaction;
            consulta.CommandText = "SELECT Nombre, Existencia FROM Productos WHERE IdProducto = $id;";
            consulta.Parameters.AddWithValue("$id", idProducto);

            using var reader = consulta.ExecuteReader();
            if (!reader.Read())
            {
                throw new ReglaNegocioException("El producto no existe.");
            }

            nombre = reader.GetString(0);
            anterior = reader.GetInt32(1);
        }

        if (existenciaEsperada is int esperada && esperada != anterior)
        {
            throw new ReglaNegocioException(
                $"La existencia de \"{nombre}\" cambió mientras se editaba (ahora es {anterior}). " +
                "Revise el valor y vuelva a intentarlo.");
        }

        if (anterior == nuevaExistencia)
        {
            return anterior;
        }

        using (var actualizar = connection.CreateCommand())
        {
            actualizar.Transaction = transaction;
            actualizar.CommandText = "UPDATE Productos SET Existencia = $existencia WHERE IdProducto = $id;";
            actualizar.Parameters.AddWithValue("$existencia", nuevaExistencia);
            actualizar.Parameters.AddWithValue("$id", idProducto);
            actualizar.ExecuteNonQuery();
        }

        using (var registrar = connection.CreateCommand())
        {
            registrar.Transaction = transaction;
            registrar.CommandText = """
                INSERT INTO AjustesInventario (FechaHora, IdProducto, NombreProducto, ExistenciaAnterior, ExistenciaNueva, Motivo)
                VALUES ($fechaHora, $id, $nombre, $anterior, $nueva, $motivo);
                """;
            registrar.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(DateTime.Now));
            registrar.Parameters.AddWithValue("$id", idProducto);
            registrar.Parameters.AddWithValue("$nombre", nombre);
            registrar.Parameters.AddWithValue("$anterior", anterior);
            registrar.Parameters.AddWithValue("$nueva", nuevaExistencia);
            registrar.Parameters.AddWithValue("$motivo", motivo);
            registrar.ExecuteNonQuery();
        }

        transaction.Commit();
        return anterior;
    }

    /// <summary>
    /// Productos activos con existencia crítica (igual o menor al stock
    /// mínimo), ordenados del más urgente al menos urgente: primero los
    /// agotados y luego por cuánto les falta para llegar al mínimo.
    /// El filtro se resuelve en SQL, sin traer todo el catálogo a memoria.
    /// </summary>
    public List<Producto> ObtenerBajoStock()
    {
        var productos = new List<Producto>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnasProducto}
            FROM Productos p
            INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria
            WHERE p.Estado = 1
              AND p.Existencia <= p.StockMinimo
            ORDER BY (p.Existencia > 0), (p.Existencia - p.StockMinimo), p.Nombre;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(Mapear(reader));
        }

        return productos;
    }

    private static void AgregarParametros(SqliteCommand command, Producto producto)
    {
        command.Parameters.AddWithValue("$nombre", producto.Nombre);
        command.Parameters.AddWithValue("$idCategoria", producto.IdCategoria);
        command.Parameters.AddWithValue("$precioCompra", Dinero.ACentavos(producto.PrecioCompra));
        command.Parameters.AddWithValue("$precioVenta", Dinero.ACentavos(producto.PrecioVenta));
        command.Parameters.AddWithValue("$stockMinimo", producto.StockMinimo);
        command.Parameters.AddWithValue("$estado", producto.Estado ? 1 : 0);
    }

    private static Producto Mapear(SqliteDataReader reader)
    {
        return new Producto
        {
            IdProducto = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            IdCategoria = reader.GetInt32(2),
            NombreCategoria = reader.GetString(3),
            PrecioCompra = reader.LeerDineroONulo(4),
            PrecioVenta = reader.LeerDinero(5),
            Existencia = reader.GetInt32(6),
            StockMinimo = reader.GetInt32(7),
            Estado = reader.GetInt32(8) == 1
        };
    }
}
