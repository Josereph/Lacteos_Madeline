using LacteosMadeline.Data;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using Microsoft.Data.Sqlite;

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
    private readonly InventarioService _inventarioService = new();
    private readonly CajaService _cajaService = new();

    // ─── Reporte de ventas ────────────────────────────────────────────────

    public List<FilaReporteVenta> ReporteVentas(DateTime? desde = null, DateTime? hasta = null)
    {
        var ventas = ObtenerVentasConFiltro(desde, hasta);
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

    private List<Venta> ObtenerVentasConFiltro(DateTime? desde, DateTime? hasta)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {VentaRepository.ColumnasVenta} FROM Ventas
            WHERE ($desde IS NULL OR FechaHora >= $desde)
              AND ($hasta IS NULL OR FechaHora <= $hasta)
            ORDER BY IdVenta;
            """;
        command.Parameters.AddWithValue("$desde", (object?)desde?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        command.Parameters.AddWithValue("$hasta", (object?)hasta?.ToString("yyyy-MM-dd 23:59:59") ?? DBNull.Value);

        var ventas = new List<Venta>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ventas.Add(VentaRepository.MapearVenta(reader));
        }
        return ventas;
    }

    // ─── Productos de mayor rotación ──────────────────────────────────────

    /// <summary>
    /// Productos ordenados por unidades vendidas en el rango de fechas.
    /// Se agrupa por IdProducto y se muestra el nombre actual del producto
    /// (o el histórico si ya no existe en el catálogo).
    /// </summary>
    public List<FilaProductoVendido> ProductosMasVendidos(DateTime? desde = null, DateTime? hasta = null)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(p.Nombre, MAX(d.NombreProducto)),
                   SUM(d.Cantidad),
                   SUM(d.Subtotal),
                   COUNT(DISTINCT d.IdVenta),
                   p.Existencia
            FROM DetalleVentas d
            INNER JOIN Ventas v ON v.IdVenta = d.IdVenta
            LEFT JOIN Productos p ON p.IdProducto = d.IdProducto
            WHERE ($desde IS NULL OR v.FechaHora >= $desde)
              AND ($hasta IS NULL OR v.FechaHora <= $hasta)
            GROUP BY d.IdProducto
            ORDER BY SUM(d.Cantidad) DESC, SUM(d.Subtotal) DESC;
            """;
        command.Parameters.AddWithValue("$desde", (object?)desde?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        command.Parameters.AddWithValue("$hasta", (object?)hasta?.ToString("yyyy-MM-dd 23:59:59") ?? DBNull.Value);

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
                TotalVendido      = reader.GetDecimal(2),
                NumeroVentas      = reader.GetInt32(3),
                ExistenciaActual  = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
            });
        }
        return filas;
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
        catch
        {
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
