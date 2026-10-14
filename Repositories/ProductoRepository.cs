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
/// compras, mermas, anulaciones o un ajuste (<see cref="AjustarExistencia"/>).
/// Modificar los datos del producto nunca la toca. Las cantidades se
/// guardan en milésimas (ver <see cref="Cantidades"/>).
/// </summary>
public class ProductoRepository
{
    private const string ColumnasProducto = """
        p.IdProducto, p.Nombre, p.IdCategoria, c.Nombre, p.PrecioCompra,
        p.PrecioVenta, p.Existencia, p.StockMinimo, p.Estado, p.Unidad, p.Codigo
        """;

    private const string DesdeProductos = """
        FROM Productos p
        INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria
        """;

    /// <summary>
    /// Registra un producto nuevo. Su existencia inicial se guarda tal cual.
    /// </summary>
    public int Registrar(Producto producto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Productos (Nombre, IdCategoria, PrecioCompra, PrecioVenta, Existencia, StockMinimo, Estado, Unidad, Codigo)
            VALUES ($nombre, $idCategoria, $precioCompra, $precioVenta, $existencia, $stockMinimo, $estado, $unidad, $codigo);
            SELECT last_insert_rowid();
            """;
        AgregarParametros(command, producto);
        command.Parameters.AddWithValue("$existencia", Cantidades.AMilesimas(producto.Existencia));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Actualiza nombre, código, categoría, unidad, precios, stock mínimo y
    /// estado. La existencia NO se modifica aquí: si el formulario tenía un
    /// valor viejo (por ejemplo, se vendió mientras se editaba),
    /// sobrescribirla borraría esos movimientos. Para corregirla use
    /// <see cref="AjustarExistencia"/>.
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
                Estado = $estado,
                Unidad = $unidad,
                Codigo = $codigo
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

    /// <summary>Busca por nombre o código.</summary>
    public List<Producto> Buscar(string filtro = "", bool soloActivos = false)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnasProducto}
            {DesdeProductos}
            WHERE (p.Nombre LIKE $filtro OR p.Codigo LIKE $filtro)
              AND (($soloActivos = 0) OR (p.Estado = 1))
            ORDER BY p.Nombre;
            """;
        command.Parameters.AddWithValue("$filtro", $"%{filtro}%");
        command.Parameters.AddWithValue("$soloActivos", soloActivos ? 1 : 0);

        return Leer(command);
    }

    public Producto? ObtenerPorId(int idProducto)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {ColumnasProducto} {DesdeProductos} WHERE p.IdProducto = $id;";
        command.Parameters.AddWithValue("$id", idProducto);

        return Leer(command).FirstOrDefault();
    }

    /// <summary>Producto con ese código exacto (lector de código de barras), o null.</summary>
    public Producto? ObtenerPorCodigo(string codigo)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {ColumnasProducto} {DesdeProductos} WHERE p.Codigo = $codigo;";
        command.Parameters.AddWithValue("$codigo", codigo.Trim());

        return Leer(command).FirstOrDefault();
    }

    /// <summary>Indica si otro producto ya usa ese código.</summary>
    public bool ExisteCodigo(string codigo, int idExcluir = 0)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Productos WHERE Codigo = $codigo AND IdProducto <> $idExcluir;";
        command.Parameters.AddWithValue("$codigo", codigo);
        command.Parameters.AddWithValue("$idExcluir", idExcluir);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Descuenta existencia dentro de una conexión y transacción ya abiertas.
    /// Se utiliza al confirmar una venta o una merma para que el descuento de
    /// inventario y el registro ocurran de forma atómica (todo o nada).
    /// La condición "Existencia >= $cantidad" en el propio UPDATE evita
    /// vender más de lo disponible (regla RN-05).
    /// </summary>
    public void DescontarExistencia(SqliteConnection connection, SqliteTransaction transaction, int idProducto, decimal cantidad)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Productos
            SET Existencia = Existencia - $cantidad
            WHERE IdProducto = $id AND Existencia >= $cantidad;
            """;
        command.Parameters.AddWithValue("$cantidad", Cantidades.AMilesimas(cantidad));
        command.Parameters.AddWithValue("$id", idProducto);

        var filasAfectadas = command.ExecuteNonQuery();
        if (filasAfectadas == 0)
        {
            throw new ReglaNegocioException("Existencia insuficiente.");
        }
    }

    /// <summary>
    /// Devuelve existencia dentro de una transacción ya abierta (anulación
    /// de una venta). Falla si el producto no existe.
    /// </summary>
    public void AumentarExistencia(SqliteConnection connection, SqliteTransaction transaction, int idProducto, decimal cantidad)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE Productos SET Existencia = Existencia + $cantidad WHERE IdProducto = $id;";
        command.Parameters.AddWithValue("$cantidad", Cantidades.AMilesimas(cantidad));
        command.Parameters.AddWithValue("$id", idProducto);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new ReglaNegocioException($"El producto #{idProducto} ya no existe.");
        }
    }

    /// <summary>
    /// Entrada de mercadería (compra) dentro de una transacción ya abierta:
    /// aumenta la existencia y actualiza el precio de compra con el costo
    /// promedio ponderado entre lo que había y lo que entra:
    ///   (costo actual × existencia + costo nuevo × cantidad) / (existencia + cantidad)
    /// Si no había existencia o costo, el costo pasa a ser el de la compra.
    /// </summary>
    public void RegistrarEntrada(SqliteConnection connection, SqliteTransaction transaction, int idProducto, decimal cantidad, decimal costoUnitario)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Productos
            SET PrecioCompra = CASE
                    WHEN PrecioCompra IS NULL OR Existencia <= 0 THEN $costo
                    ELSE CAST(ROUND((PrecioCompra * Existencia + $costo * $cantidad) * 1.0 / (Existencia + $cantidad)) AS INTEGER)
                END,
                Existencia = Existencia + $cantidad
            WHERE IdProducto = $id;
            """;
        command.Parameters.AddWithValue("$cantidad", Cantidades.AMilesimas(cantidad));
        command.Parameters.AddWithValue("$costo", Dinero.ACentavos(costoUnitario));
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
    public decimal AjustarExistencia(int idProducto, decimal nuevaExistencia, string motivo,
        decimal? existenciaEsperada = null, int? idUsuario = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var transaction = connection.BeginTransaction();

        string nombre;
        decimal anterior;
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
            anterior = reader.LeerCantidad(1);
        }

        if (existenciaEsperada is decimal esperada && Cantidades.Redondear(esperada) != anterior)
        {
            throw new ReglaNegocioException(
                $"La existencia de \"{nombre}\" cambió mientras se editaba (ahora es {Cantidades.ParaEditar(anterior)}). " +
                "Revise el valor y vuelva a intentarlo.");
        }

        if (anterior == Cantidades.Redondear(nuevaExistencia))
        {
            return anterior;
        }

        using (var actualizar = connection.CreateCommand())
        {
            actualizar.Transaction = transaction;
            actualizar.CommandText = "UPDATE Productos SET Existencia = $existencia WHERE IdProducto = $id;";
            actualizar.Parameters.AddWithValue("$existencia", Cantidades.AMilesimas(nuevaExistencia));
            actualizar.Parameters.AddWithValue("$id", idProducto);
            actualizar.ExecuteNonQuery();
        }

        using (var registrar = connection.CreateCommand())
        {
            registrar.Transaction = transaction;
            registrar.CommandText = """
                INSERT INTO AjustesInventario (FechaHora, IdProducto, NombreProducto, ExistenciaAnterior, ExistenciaNueva, Motivo, IdUsuario)
                VALUES ($fechaHora, $id, $nombre, $anterior, $nueva, $motivo, $idUsuario);
                """;
            registrar.Parameters.AddWithValue("$fechaHora", Fechas.ParaBaseDatos(DateTime.Now));
            registrar.Parameters.AddWithValue("$id", idProducto);
            registrar.Parameters.AddWithValue("$nombre", nombre);
            registrar.Parameters.AddWithValue("$anterior", Cantidades.AMilesimas(anterior));
            registrar.Parameters.AddWithValue("$nueva", Cantidades.AMilesimas(nuevaExistencia));
            registrar.Parameters.AddWithValue("$motivo", motivo);
            registrar.Parameters.AddWithValue("$idUsuario", (object?)idUsuario ?? DBNull.Value);
            registrar.ExecuteNonQuery();
        }

        transaction.Commit();
        return anterior;
    }

    /// <summary>Ajustes de inventario del rango de fechas, más recientes primero.</summary>
    public List<AjusteInventario> ObtenerAjustes(DateTime? desde, DateTime? hasta)
    {
        var ajustes = new List<AjusteInventario>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.IdAjuste, a.FechaHora, a.NombreProducto, a.ExistenciaAnterior, a.ExistenciaNueva, a.Motivo, u.Nombre
            FROM AjustesInventario a
            LEFT JOIN Usuarios u ON u.IdUsuario = a.IdUsuario
            WHERE a.FechaHora BETWEEN $desde AND $hasta
            ORDER BY a.FechaHora DESC, a.IdAjuste DESC;
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ajustes.Add(new AjusteInventario
            {
                IdAjuste = reader.GetInt32(0),
                FechaHora = Fechas.DesdeBaseDatos(reader.GetString(1)),
                NombreProducto = reader.GetString(2),
                ExistenciaAnterior = reader.LeerCantidad(3),
                ExistenciaNueva = reader.LeerCantidad(4),
                Motivo = reader.GetString(5),
                NombreUsuario = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return ajustes;
    }

    /// <summary>
    /// Productos activos con existencia crítica (igual o menor al stock
    /// mínimo), ordenados del más urgente al menos urgente: primero los
    /// agotados y luego por cuánto les falta para llegar al mínimo.
    /// El filtro se resuelve en SQL, sin traer todo el catálogo a memoria.
    /// </summary>
    public List<Producto> ObtenerBajoStock()
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnasProducto}
            {DesdeProductos}
            WHERE p.Estado = 1
              AND p.Existencia <= p.StockMinimo
            ORDER BY (p.Existencia > 0), (p.Existencia - p.StockMinimo), p.Nombre;
            """;

        return Leer(command);
    }

    private static List<Producto> Leer(SqliteCommand command)
    {
        var productos = new List<Producto>();
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
        command.Parameters.AddWithValue("$stockMinimo", Cantidades.AMilesimas(producto.StockMinimo));
        command.Parameters.AddWithValue("$estado", producto.Estado ? 1 : 0);
        command.Parameters.AddWithValue("$unidad", producto.Unidad);
        command.Parameters.AddWithValue("$codigo", string.IsNullOrWhiteSpace(producto.Codigo) ? DBNull.Value : producto.Codigo.Trim());
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
            Existencia = reader.LeerCantidad(6),
            StockMinimo = reader.LeerCantidad(7),
            Estado = reader.GetInt32(8) == 1,
            Unidad = reader.GetString(9),
            Codigo = reader.IsDBNull(10) ? null : reader.GetString(10)
        };
    }
}
