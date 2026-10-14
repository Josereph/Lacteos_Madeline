using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Genera los datos para los reportes de ventas, compras, inventario,
/// utilidad y caja. Los filtros de fecha son opcionales. Las ventas
/// anuladas aparecen en el reporte de ventas marcadas como tales, pero no
/// suman en totales, productos más vendidos ni utilidad.
/// </summary>
public class ReporteService
{
    private readonly VentaRepository _ventaRepository = new();
    private readonly CompraRepository _compraRepository = new();
    private readonly MermaRepository _mermaRepository = new();
    private readonly ProductoRepository _productoRepository = new();
    private readonly InventarioService _inventarioService = new();
    private readonly CajaService _cajaService = new();

    // ─── Reporte de ventas ────────────────────────────────────────────────

    public List<FilaReporteVenta> ReporteVentas(DateTime? desde = null, DateTime? hasta = null)
    {
        var ventas = _ventaRepository.ObtenerPorRango(desde, hasta, cronologico: true);
        decimal acumulado = 0;

        return ventas.Select(v =>
        {
            if (!v.EsAnulada) acumulado += v.Total;
            return new FilaReporteVenta
            {
                IdVenta    = v.IdVenta,
                FechaHora  = v.FechaHora,
                Estado     = v.Estado,
                Cliente    = v.NombreCliente ?? Cliente.ConsumidorFinal,
                Usuario    = v.NombreUsuario,
                MetodoPago = v.MetodoPago,
                Descuento  = v.Descuento,
                Total      = v.Total,
                Acumulado  = acumulado
            };
        }).ToList();
    }

    // ─── Productos de mayor rotación ──────────────────────────────────────

    /// <summary>
    /// Productos ordenados por cantidad vendida en el rango de fechas (sin
    /// ventas anuladas). Se agrupa por IdProducto y se muestra el nombre
    /// actual del producto (o el histórico si ya no existe en el catálogo).
    /// Plan de ejecución: primero se ubican las ventas del rango con
    /// IX_Ventas_FechaHora, luego su detalle con IX_DetalleVentas_Venta y
    /// recién al final se agrupa; nunca se recorre todo DetalleVentas.
    /// </summary>
    public List<FilaProductoVendido> ProductosMasVendidos(DateTime? desde = null, DateTime? hasta = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            WITH VentasRango AS (
                SELECT IdVenta FROM Ventas
                WHERE FechaHora BETWEEN $desde AND $hasta AND Estado = 'Activa'
            ),
            Vendidos AS (
                SELECT d.IdProducto,
                       MAX(d.NombreProducto)     AS NombreHistorico,
                       MAX(d.Unidad)             AS UnidadHistorica,
                       SUM(d.Cantidad)           AS Cantidad,
                       SUM(d.Subtotal)           AS Importe,
                       COUNT(DISTINCT d.IdVenta) AS NumVentas
                FROM VentasRango v
                INNER JOIN DetalleVentas d ON d.IdVenta = v.IdVenta
                GROUP BY d.IdProducto
            )
            SELECT COALESCE(p.Nombre, x.NombreHistorico), COALESCE(p.Unidad, x.UnidadHistorica),
                   x.Cantidad, x.Importe, x.NumVentas, p.Existencia
            FROM Vendidos x
            LEFT JOIN Productos p ON p.IdProducto = x.IdProducto
            ORDER BY x.Cantidad DESC, x.Importe DESC;
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        var filas = new List<FilaProductoVendido>();
        using var reader = command.ExecuteReader();
        var posicion = 1;
        while (reader.Read())
        {
            filas.Add(new FilaProductoVendido
            {
                Posicion          = posicion++,
                Producto          = reader.GetString(0),
                Unidad            = reader.GetString(1),
                UnidadesVendidas  = reader.LeerCantidad(2),
                TotalVendido      = reader.LeerDinero(3),
                NumeroVentas      = reader.GetInt32(4),
                ExistenciaActual  = reader.IsDBNull(5) ? 0 : reader.LeerCantidad(5)
            });
        }
        return filas;
    }

    // ─── Utilidad ─────────────────────────────────────────────────────────

    /// <summary>
    /// Utilidad bruta por producto: lo vendido menos su costo (el costo de
    /// compra vigente al momento de cada venta). Sin ventas anuladas. Las
    /// líneas sin costo registrado se marcan como "costo incompleto".
    /// </summary>
    public List<FilaUtilidad> ReporteUtilidad(DateTime? desde = null, DateTime? hasta = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            WITH VentasRango AS (
                SELECT IdVenta FROM Ventas
                WHERE FechaHora BETWEEN $desde AND $hasta AND Estado = 'Activa'
            ),
            PorProducto AS (
                SELECT d.IdProducto,
                       MAX(d.NombreProducto) AS NombreHistorico,
                       MAX(d.Unidad)         AS Unidad,
                       SUM(d.Cantidad)       AS Cantidad,
                       SUM(d.Subtotal)       AS Ventas,
                       SUM(CASE WHEN d.CostoUnitario IS NULL THEN 0
                                ELSE CAST(ROUND(d.CostoUnitario * d.Cantidad / 1000.0) AS INTEGER) END) AS Costo,
                       SUM(CASE WHEN d.CostoUnitario IS NULL THEN 1 ELSE 0 END) AS LineasSinCosto
                FROM VentasRango v
                INNER JOIN DetalleVentas d ON d.IdVenta = v.IdVenta
                GROUP BY d.IdProducto
            )
            SELECT COALESCE(p.Nombre, x.NombreHistorico), x.Unidad, x.Cantidad, x.Ventas, x.Costo, x.LineasSinCosto
            FROM PorProducto x
            LEFT JOIN Productos p ON p.IdProducto = x.IdProducto
            ORDER BY x.Ventas - x.Costo DESC;
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);

        var filas = new List<FilaUtilidad>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var ventas = reader.LeerDinero(3);
            var costo = reader.LeerDinero(4);
            filas.Add(new FilaUtilidad
            {
                Producto = reader.GetString(0),
                Unidad = reader.GetString(1),
                Cantidad = reader.LeerCantidad(2),
                Ventas = ventas,
                Costo = costo,
                Utilidad = ventas - costo,
                MargenPorcentaje = ventas == 0 ? 0 : Math.Round((ventas - costo) * 100m / ventas, 1),
                CostoIncompleto = reader.GetInt32(5) > 0
            });
        }

        return filas;
    }

    /// <summary>Total de descuentos de las ventas activas del rango (se restan de la utilidad).</summary>
    public decimal TotalDescuentos(DateTime? desde = null, DateTime? hasta = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(Descuento), 0) FROM Ventas
            WHERE FechaHora BETWEEN $desde AND $hasta AND Estado = 'Activa';
            """;
        RangoFechas.AgregarParametros(command, desde, hasta);
        return Dinero.DesdeCentavos(Convert.ToInt64(command.ExecuteScalar()));
    }

    // ─── Mermas y ajustes ─────────────────────────────────────────────────

    /// <summary>
    /// Bajas por caducidad/daño del período. No forman parte del reporte
    /// de ventas ni del arqueo de caja.
    /// </summary>
    public List<Merma> ReporteMermas(DateTime? desde = null, DateTime? hasta = null)
    {
        return _mermaRepository.ObtenerPorRango(desde, hasta);
    }

    /// <summary>Correcciones manuales de existencia del período.</summary>
    public List<AjusteInventario> ReporteAjustes(DateTime? desde = null, DateTime? hasta = null)
    {
        return _productoRepository.ObtenerAjustes(desde, hasta);
    }

    // ─── Reporte de compras ───────────────────────────────────────────────

    public List<Compra> ReporteCompras(DateTime? desde = null, DateTime? hasta = null)
    {
        return _compraRepository.ObtenerHistorial(desde, hasta);
    }

    // ─── Reporte de inventario ────────────────────────────────────────────

    public List<Producto> ReporteInventario()
    {
        return _inventarioService.ConsultarDisponibles();
    }

    public List<Producto> ProductosBajoStock()
    {
        return _inventarioService.ConsultarBajoStock();
    }

    // ─── Resumen de caja ──────────────────────────────────────────────────

    public ResumenCaja? ResumenCajaPorId(int idCaja)
    {
        try
        {
            return _cajaService.ObtenerResumen(idCaja);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Resumen de caja #{idCaja} para reportes", ex);
            return null;
        }
    }
}

// ─── DTOs de reporte ──────────────────────────────────────────────────────

public class FilaReporteVenta
{
    public int IdVenta { get; set; }
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = Venta.EstadoActiva;
    public string Cliente { get; set; } = string.Empty;
    public string? Usuario { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public decimal Acumulado { get; set; }
}

public class FilaProductoVendido
{
    public int Posicion { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Unidad { get; set; } = UnidadMedida.Unidad;
    public decimal UnidadesVendidas { get; set; }
    public decimal TotalVendido { get; set; }
    public int NumeroVentas { get; set; }
    public decimal ExistenciaActual { get; set; }
}

public class FilaUtilidad
{
    public string Producto { get; set; } = string.Empty;
    public string Unidad { get; set; } = UnidadMedida.Unidad;
    public decimal Cantidad { get; set; }
    public decimal Ventas { get; set; }
    public decimal Costo { get; set; }
    public decimal Utilidad { get; set; }
    public decimal MargenPorcentaje { get; set; }
    public bool CostoIncompleto { get; set; }
}
