using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Genera los datos para los reportes de ventas, compras, inventario
/// y caja. Los reportes son sencillos y directos, sin análisis complejo
/// (sección 17 lineamientos). Los filtros de fecha son opcionales.
/// </summary>
public class ReporteService
{
    private readonly VentaRepository _ventaRepository = new();
    private readonly CompraRepository _compraRepository = new();
    private readonly MermaRepository _mermaRepository = new();
    private readonly InventarioService _inventarioService = new();
    private readonly CajaService _cajaService = new();

    // ─── Reporte de ventas ────────────────────────────────────────────────

    public List<FilaReporteVenta> ReporteVentas(DateTime? desde = null, DateTime? hasta = null)
    {
        var ventas = _ventaRepository.ObtenerPorRango(desde, hasta, cronologico: true);
        decimal acumulado = 0;

        return ventas.Select(v =>
        {
            acumulado += v.Total;
            return new FilaReporteVenta
            {
                IdVenta    = v.IdVenta,
                FechaHora  = v.FechaHora,
                MetodoPago = v.MetodoPago,
                Total      = v.Total,
                Acumulado  = acumulado
            };
        }).ToList();
    }

    // ─── Productos de mayor rotación ──────────────────────────────────────

    /// <summary>
    /// Productos ordenados por unidades vendidas en el rango de fechas.
    /// Se agrupa por IdProducto y se muestra el nombre actual del producto
    /// (o el histórico si ya no existe en el catálogo).
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
                WHERE FechaHora BETWEEN $desde AND $hasta
            ),
            Vendidos AS (
                SELECT d.IdProducto,
                       MAX(d.NombreProducto)     AS NombreHistorico,
                       SUM(d.Cantidad)           AS Unidades,
                       SUM(d.Subtotal)           AS Importe,
                       COUNT(DISTINCT d.IdVenta) AS NumVentas
                FROM VentasRango v
                INNER JOIN DetalleVentas d ON d.IdVenta = v.IdVenta
                GROUP BY d.IdProducto
            )
            SELECT COALESCE(p.Nombre, x.NombreHistorico),
                   x.Unidades, x.Importe, x.NumVentas, p.Existencia
            FROM Vendidos x
            LEFT JOIN Productos p ON p.IdProducto = x.IdProducto
            ORDER BY x.Unidades DESC, x.Importe DESC;
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
                UnidadesVendidas  = reader.GetInt32(1),
                TotalVendido      = reader.LeerDinero(2),
                NumeroVentas      = reader.GetInt32(3),
                ExistenciaActual  = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
            });
        }
        return filas;
    }

    // ─── Mermas ───────────────────────────────────────────────────────────

    /// <summary>
    /// Bajas por caducidad/daño del período. No forman parte del reporte
    /// de ventas ni del arqueo de caja.
    /// </summary>
    public List<Merma> ReporteMermas(DateTime? desde = null, DateTime? hasta = null)
    {
        return _mermaRepository.ObtenerPorRango(desde, hasta);
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
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Acumulado { get; set; }
}

public class FilaProductoVendido
{
    public int Posicion { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int UnidadesVendidas { get; set; }
    public decimal TotalVendido { get; set; }
    public int NumeroVentas { get; set; }
    public int ExistenciaActual { get; set; }
}
