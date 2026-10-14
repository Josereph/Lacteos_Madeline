using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using LacteosMadeline.Models;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Construye el texto del ticket de venta e imprime. El ticket es un
/// comprobante de control interno; el sistema NO maneja DTE, firma
/// electrónica ni integraciones con Hacienda (sección 9 de los lineamientos).
///
/// El texto se arma a un ancho fijo de caracteres
/// (<see cref="ConfiguracionNegocio.AnchoTicket"/>) con fuente monoespaciada,
/// y al imprimir la letra se ajusta al ancho del papel, de modo que funciona
/// igual en impresoras térmicas de 58 mm, de 80 mm o en papel carta.
/// </summary>
public static class TicketHelper
{
    private const string FuenteTicket = "Consolas";
    private const float TamanoMaximo = 10f;
    private const float TamanoMinimo = 6f;

    public static string GenerarTexto(Venta venta)
    {
        var ancho = ConfiguracionNegocio.AnchoTicket;
        var cultura = ConfiguracionNegocio.Cultura;
        var separador = new string('-', ancho);
        var sb = new StringBuilder();

        sb.AppendLine(Centrar(ConfiguracionNegocio.NombreNegocio, ancho));
        foreach (var dato in new[] { ConfiguracionNegocio.Direccion, ConfiguracionNegocio.Telefono })
        {
            foreach (var linea in Ajustar(dato, ancho))
            {
                sb.AppendLine(Centrar(linea, ancho));
            }
        }

        sb.AppendLine(separador);
        sb.AppendLine($"Venta No: {venta.IdVenta}");
        sb.AppendLine($"Fecha:    {venta.FechaHora.ToString("dd/MM/yyyy", cultura)}");
        sb.AppendLine($"Hora:     {venta.FechaHora.ToString("HH:mm:ss", cultura)}");
        sb.AppendLine(separador);

        foreach (var detalle in venta.Detalles)
        {
            foreach (var linea in Ajustar(detalle.NombreProducto, ancho))
            {
                sb.AppendLine(linea);
            }

            var calculo = $"  {detalle.Cantidad} x {Monto(detalle.PrecioUnitario)}";
            sb.AppendLine(Columnas(calculo, Monto(detalle.Subtotal), ancho));
        }

        sb.AppendLine(separador);
        sb.AppendLine(Columnas("TOTAL:", Monto(venta.Total), ancho));
        sb.AppendLine(Columnas("Pago:", venta.MetodoPago, ancho));
        if (venta.MontoRecibido is decimal recibido)
        {
            sb.AppendLine(Columnas("Recibido:", Monto(recibido), ancho));
            sb.AppendLine(Columnas("Cambio:", Monto(venta.Cambio ?? 0), ancho));
        }

        sb.AppendLine(separador);
        foreach (var linea in Ajustar(ConfiguracionNegocio.MensajeTicket, ancho))
        {
            sb.AppendLine(Centrar(linea, ancho));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Envía el ticket a la impresora predeterminada. Si no hay impresora
    /// instalada o falla la impresión, devuelve el motivo en lugar de
    /// lanzar una excepción: la venta ya está registrada y no debe perderse.
    /// </summary>
    public static (bool Exitoso, string Mensaje) Imprimir(Venta venta)
    {
        var lineas = GenerarTexto(venta).TrimEnd().Split(Environment.NewLine);

        try
        {
            using var documento = new PrintDocument();
            if (!documento.PrinterSettings.IsValid)
            {
                return (false, "No hay una impresora predeterminada configurada en Windows.");
            }

            documento.DocumentName = $"Ticket venta {venta.IdVenta}";
            documento.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);

            var siguienteLinea = 0;
            documento.PrintPage += (_, e) =>
            {
                if (e.Graphics is null) return;

                using var fuente = FuenteQueCabe(e.Graphics, e.MarginBounds.Width);
                var altoLinea = fuente.GetHeight(e.Graphics);
                var y = (float)e.MarginBounds.Top;

                while (siguienteLinea < lineas.Length && y + altoLinea <= e.MarginBounds.Bottom)
                {
                    e.Graphics.DrawString(lineas[siguienteLinea], fuente, Brushes.Black, e.MarginBounds.Left, y);
                    y += altoLinea;
                    siguienteLinea++;
                }

                e.HasMorePages = siguienteLinea < lineas.Length;
            };

            documento.Print();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Impresión de ticket, venta #{venta.IdVenta}", ex);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Fuente monoespaciada del mayor tamaño (hasta 10 pt) en que una línea
    /// completa del ticket cabe en el ancho imprimible del papel.
    /// </summary>
    private static Font FuenteQueCabe(Graphics graficos, int anchoDisponible)
    {
        var muestra = new string('M', ConfiguracionNegocio.AnchoTicket);
        using var referencia = new Font(FuenteTicket, TamanoMaximo);
        var anchoMuestra = graficos.MeasureString(muestra, referencia, int.MaxValue, StringFormat.GenericTypographic).Width;

        var tamano = anchoMuestra <= 0
            ? TamanoMaximo
            : Math.Clamp(TamanoMaximo * anchoDisponible / anchoMuestra, TamanoMinimo, TamanoMaximo);

        return new Font(FuenteTicket, tamano);
    }

    private static string Monto(decimal valor) => valor.ToString("C2", ConfiguracionNegocio.Cultura);

    /// <summary>Texto a la izquierda y valor alineado a la derecha.</summary>
    private static string Columnas(string izquierda, string derecha, int ancho)
    {
        var espacio = ancho - izquierda.Length - derecha.Length;
        return espacio >= 1
            ? izquierda + new string(' ', espacio) + derecha
            : izquierda + Environment.NewLine + derecha.PadLeft(ancho);
    }

    private static string Centrar(string texto, int ancho)
    {
        if (texto.Length >= ancho) return texto;
        return texto.PadLeft((ancho + texto.Length) / 2);
    }

    /// <summary>Divide el texto en líneas de como máximo <paramref name="ancho"/> caracteres, respetando palabras.</summary>
    private static IEnumerable<string> Ajustar(string? texto, int ancho)
    {
        if (string.IsNullOrWhiteSpace(texto)) yield break;

        var linea = new StringBuilder();
        foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var resto = palabra;
            while (resto.Length > ancho)
            {
                if (linea.Length > 0)
                {
                    yield return linea.ToString();
                    linea.Clear();
                }

                yield return resto[..ancho];
                resto = resto[ancho..];
            }

            if (linea.Length > 0 && linea.Length + 1 + resto.Length > ancho)
            {
                yield return linea.ToString();
                linea.Clear();
            }

            if (linea.Length > 0) linea.Append(' ');
            linea.Append(resto);
        }

        if (linea.Length > 0) yield return linea.ToString();
    }
}
