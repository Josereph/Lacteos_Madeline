using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using LacteosMadeline.Models;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Construye el texto del ticket de venta e imprime. El ticket es un
/// comprobante de control interno; el sistema NO maneja DTE, firma
/// electrónica ni integraciones con Hacienda (sección 9 de los lineamientos).
/// </summary>
public static class TicketHelper
{
    public static string GenerarTexto(Venta venta)
    {
        var sb = new StringBuilder();

        sb.AppendLine("      LACTEOS MADELINE");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Venta No: {venta.IdVenta}");
        sb.AppendLine($"Fecha:    {venta.FechaHora:dd/MM/yyyy}");
        sb.AppendLine($"Hora:     {venta.FechaHora:HH:mm:ss}");
        sb.AppendLine("--------------------------------");

        foreach (var detalle in venta.Detalles)
        {
            sb.AppendLine(detalle.NombreProducto);
            sb.AppendLine($"  {detalle.Cantidad} x {detalle.PrecioUnitario:C2}  = {detalle.Subtotal:C2}");
        }

        sb.AppendLine("--------------------------------");
        sb.AppendLine($"TOTAL: {venta.Total:C2}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine("     Gracias por su compra.");

        return sb.ToString();
    }

    public static void Imprimir(Venta venta)
    {
        var textoTicket = GenerarTexto(venta);

        using var documento = new PrintDocument();
        documento.PrintPage += (sender, e) =>
        {
            using var fuente = new Font("Consolas", 10);
            e.Graphics?.DrawString(textoTicket, fuente, Brushes.Black, 10, 10);
        };

        documento.Print();
    }
}
