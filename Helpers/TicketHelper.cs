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
        sb.AppendLine($"Pago:     {venta.MetodoPago}");
        if (venta.MontoRecibido is decimal recibido)
        {
            sb.AppendLine($"Recibido: {recibido:C2}");
            sb.AppendLine($"Cambio:   {venta.Cambio ?? 0:C2}");
        }
        sb.AppendLine("--------------------------------");
        sb.AppendLine("     Gracias por su compra.");

        return sb.ToString();
    }

    /// <summary>
    /// Envía el ticket a la impresora predeterminada. Si no hay impresora
    /// instalada o falla la impresión, devuelve el motivo en lugar de
    /// lanzar una excepción: la venta ya está registrada y no debe perderse.
    /// </summary>
    public static (bool Exitoso, string Mensaje) Imprimir(Venta venta)
    {
        var textoTicket = GenerarTexto(venta);

        try
        {
            using var documento = new PrintDocument();
            if (!documento.PrinterSettings.IsValid)
            {
                return (false, "No hay una impresora predeterminada configurada en Windows.");
            }

            documento.PrintPage += (sender, e) =>
            {
                using var fuente = new Font("Consolas", 10);
                e.Graphics?.DrawString(textoTicket, fuente, Brushes.Black, 10, 10);
            };

            documento.Print();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
