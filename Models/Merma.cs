using System.ComponentModel;
using LacteosMadeline.Helpers;

namespace LacteosMadeline.Models;

/// <summary>
/// Baja de inventario por caducidad, daño u otro motivo (producto que se
/// pierde sin venderse). Descuenta existencias pero no es una venta ni
/// mueve dinero de caja. Conserva el nombre y el costo del producto al
/// momento de la baja para valorar la pérdida.
/// </summary>
public class Merma
{
    public const string MotivoCaducidad = "Caducidad";
    public const string MotivoDanio = "Daño";
    public const string MotivoOtro = "Otro";

    public static readonly string[] Motivos = { MotivoCaducidad, MotivoDanio, MotivoOtro };

    public int IdMerma { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Motivo { get; set; } = MotivoCaducidad;
    public decimal? CostoUnitario { get; set; }
    public string? Observacion { get; set; }
    public decimal? CostoTotal => CostoUnitario is decimal costo ? Dinero.Redondear(costo * Cantidad) : null;

    [Browsable(false)] public int? IdUsuario { get; set; }
}
