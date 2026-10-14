using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Construye el texto del ticket de venta y del corte de caja, e imprime.
/// El ticket es un comprobante de control interno; el sistema NO maneja
/// DTE, firma electrónica ni integraciones con Hacienda (sección 9 de los
/// lineamientos).
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

        Encabezado(sb, ancho);

        if (venta.EsAnulada)
        {
            sb.AppendLine(Centrar("*** VENTA ANULADA ***", ancho));
        }

        sb.AppendLine(separador);
        sb.AppendLine($"Venta No: {venta.IdVenta}");
        sb.AppendLine($"Fecha:    {venta.FechaHora.ToString("dd/MM/yyyy", cultura)}");
        sb.AppendLine($"Hora:     {venta.FechaHora.ToString("HH:mm:ss", cultura)}");
        if (!string.IsNullOrWhiteSpace(venta.NombreUsuario))
        {
            Lineas(sb, $"Atendió:  {venta.NombreUsuario}", ancho);
        }

        Lineas(sb, $"Cliente:  {venta.NombreCliente ?? Cliente.ConsumidorFinal}", ancho);
        sb.AppendLine(separador);

        foreach (var detalle in venta.Detalles)
        {
            Lineas(sb, detalle.NombreProducto, ancho);
            var calculo = $"  {Cantidades.Formatear(detalle.Cantidad, detalle.Unidad)} x {Monto(detalle.PrecioUnitario)}";
            sb.AppendLine(Columnas(calculo, Monto(detalle.Subtotal), ancho));
        }

        sb.AppendLine(separador);
        if (venta.Descuento > 0)
        {
            sb.AppendLine(Columnas("Subtotal:", Monto(venta.Subtotal), ancho));
            sb.AppendLine(Columnas("Descuento:", "-" + Monto(venta.Descuento), ancho));
        }

        sb.AppendLine(Columnas("TOTAL:", Monto(venta.Total), ancho));
        sb.AppendLine(Columnas("Pago:", venta.MetodoPago, ancho));
        if (venta.MontoRecibido is decimal recibido)
        {
            sb.AppendLine(Columnas("Recibido:", Monto(recibido), ancho));
            sb.AppendLine(Columnas("Cambio:", Monto(venta.Cambio ?? 0), ancho));
        }

        if (venta.EsAnulada)
        {
            sb.AppendLine(separador);
            sb.AppendLine($"Anulada:  {venta.FechaAnulacion?.ToString("dd/MM/yyyy HH:mm", cultura)}");
            Lineas(sb, $"Motivo: {venta.MotivoAnulacion}", ancho);
        }

        sb.AppendLine(separador);
        foreach (var linea in Ajustar(ConfiguracionNegocio.MensajeTicket, ancho))
        {
            sb.AppendLine(Centrar(linea, ancho));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Corte de caja: apertura y cierre, ventas por método de pago,
    /// descuentos, anulaciones, movimientos y arqueo (esperado, contado y
    /// diferencia). Sirve para la caja abierta (corte parcial) o cerrada.
    /// </summary>
    public static string GenerarCorteCaja(ResumenCaja resumen)
    {
        var ancho = ConfiguracionNegocio.AnchoTicket;
        var cultura = ConfiguracionNegocio.Cultura;
        var separador = new string('-', ancho);
        var caja = resumen.Caja;
        var sb = new StringBuilder();

        Encabezado(sb, ancho);
        sb.AppendLine(separador);
        sb.AppendLine(Centrar(caja.Estado == "Cerrada" ? "CORTE DE CAJA" : "CORTE PARCIAL (CAJA ABIERTA)", ancho));
        sb.AppendLine($"Caja No:  {caja.IdCaja}");
        Lineas(sb, $"Apertura: {caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", cultura)}" +
                   (caja.NombreUsuarioApertura is null ? string.Empty : $" ({caja.NombreUsuarioApertura})"), ancho);
        if (caja.FechaCierre is DateTime cierre)
        {
            Lineas(sb, $"Cierre:   {cierre.ToString("dd/MM/yyyy HH:mm", cultura)}" +
                       (caja.NombreUsuarioCierre is null ? string.Empty : $" ({caja.NombreUsuarioCierre})"), ancho);
        }

        sb.AppendLine(separador);
        sb.AppendLine(Columnas("Ventas:", resumen.NumeroVentas.ToString(cultura), ancho));
        sb.AppendLine(Columnas("Anuladas:", resumen.VentasAnuladas.ToString(cultura), ancho));
        sb.AppendLine(Columnas("Descuentos:", Monto(resumen.TotalDescuentos), ancho));
        sb.AppendLine(Columnas("Ventas efectivo:", Monto(resumen.VentasEfectivo), ancho));
        sb.AppendLine(Columnas("Tarjeta/transf.:", Monto(resumen.VentasOtrosMetodos), ancho));
        sb.AppendLine(Columnas("TOTAL VENDIDO:", Monto(resumen.TotalVentas), ancho));

        sb.AppendLine(separador);
        sb.AppendLine(Columnas("Fondo inicial:", Monto(caja.MontoInicial), ancho));
        sb.AppendLine(Columnas("+ Ventas efectivo:", Monto(resumen.VentasEfectivo), ancho));
        sb.AppendLine(Columnas("+ Entradas:", Monto(resumen.TotalEntradas), ancho));
        sb.AppendLine(Columnas("- Salidas:", Monto(resumen.TotalSalidas), ancho));
        sb.AppendLine(Columnas("EFECTIVO ESPERADO:", Monto(caja.MontoEsperado ?? resumen.SaldoEsperado), ancho));
        if (caja.MontoFinal is decimal contado)
        {
            sb.AppendLine(Columnas("Efectivo contado:", Monto(contado), ancho));
            sb.AppendLine(Columnas("DIFERENCIA:", Monto(caja.Diferencia ?? 0), ancho));
        }

        if (resumen.Movimientos.Count > 0)
        {
            sb.AppendLine(separador);
            sb.AppendLine("Movimientos:");
            foreach (var m in resumen.Movimientos)
            {
                Lineas(sb, $"{m.FechaHora.ToString("HH:mm", cultura)} {m.Tipo} {m.Descripcion}", ancho);
                sb.AppendLine(Columnas(string.Empty, (m.Tipo == "Salida" ? "-" : "+") + Monto(m.Monto), ancho));
            }
        }

        sb.AppendLine(separador);
        sb.AppendLine($"Impreso: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", cultura)}");
        sb.AppendLine();
        sb.AppendLine("Firma: _______________________");

        return sb.ToString();
    }

    /// <summary>
    /// Envía el ticket a la impresora predeterminada. Si no hay impresora
    /// instalada o falla la impresión, devuelve el motivo en lugar de
    /// lanzar una excepción: la venta ya está registrada y no debe perderse.
    /// </summary>
    public static (bool Exitoso, string Mensaje) Imprimir(Venta venta) =>
        ImprimirTexto(GenerarTexto(venta), $"Ticket venta {venta.IdVenta}");

    /// <summary>Imprime cualquier texto del ancho del ticket (ticket, corte de caja…).</summary>
    public static (bool Exitoso, string Mensaje) ImprimirTexto(string texto, string nombreDocumento)
    {
        var lineas = texto.TrimEnd().Split(Environment.NewLine);

        try
        {
            using var documento = new PrintDocument();
            if (!documento.PrinterSettings.IsValid)
            {
                return (false, "No hay una impresora predeterminada configurada en Windows.");
            }

            documento.DocumentName = nombreDocumento;
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
            LogHelper.Error($"Impresión de \"{nombreDocumento}\"", ex);
            return (false, ex.Message);
        }
    }

    private static void Encabezado(StringBuilder sb, int ancho)
    {
        foreach (var linea in Ajustar(ConfiguracionNegocio.NombreNegocio, ancho))
        {
            sb.AppendLine(Centrar(linea, ancho));
        }

        var documento = string.IsNullOrWhiteSpace(ConfiguracionNegocio.Documento) ? string.Empty : $"NIT: {ConfiguracionNegocio.Documento}";
        foreach (var dato in new[] { ConfiguracionNegocio.Direccion, ConfiguracionNegocio.Telefono, documento })
        {
            foreach (var linea in Ajustar(dato, ancho))
            {
                sb.AppendLine(Centrar(linea, ancho));
            }
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

    private static void Lineas(StringBuilder sb, string texto, int ancho)
    {
        foreach (var linea in Ajustar(texto, ancho))
        {
            sb.AppendLine(linea);
        }
    }

    /// <summary>Texto a la izquierda y valor alineado a la derecha.</summary>
    private static string Columnas(string izquierda, string derecha, int ancho)
    {
        var espacio = ancho - izquierda.Length - derecha.Length;
        return espacio >= 1 || izquierda.Length == 0
            ? izquierda + new string(' ', Math.Max(espacio, 0)) + derecha
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
