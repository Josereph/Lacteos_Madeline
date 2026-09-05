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
                Total      = v.Total,
                Acumulado  = acumulado
            };
        }).ToList();
    }

    private List<Venta> ObtenerVentasConFiltro(DateTime? desde, DateTime? hasta)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdVenta, FechaHora, Total FROM Ventas
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
            ventas.Add(new Venta
            {
                IdVenta   = reader.GetInt32(0),
                FechaHora = DateTime.Parse(reader.GetString(1)),
                Total     = reader.GetDecimal(2)
            });
        }
        return ventas;
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
    public decimal Total { get; set; }
    public decimal Acumulado { get; set; }
}
