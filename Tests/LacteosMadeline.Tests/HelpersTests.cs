using System.Globalization;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;

namespace LacteosMadeline.Tests;

/// <summary>
/// Conversiones que no dependen de la base de datos: montos escritos por
/// el usuario, fechas y el texto del ticket. Deben dar el mismo resultado
/// con cualquier configuración regional de Windows.
/// </summary>
public class HelpersTests
{
    [Theory]
    [InlineData("3.50", "3.50")]
    [InlineData("3,50", "3.50")]
    [InlineData(" $ 12 ", "12")]
    [InlineData("1,250.75", "1250.75")]
    [InlineData("1.250,75", "1250.75")]
    [InlineData("0.1", "0.1")]
    public void Dinero_AceptaPuntoOComaComoDecimal(string texto, string esperado)
    {
        Assert.True(Dinero.TryParse(texto, out var monto));
        Assert.Equal(decimal.Parse(esperado, CultureInfo.InvariantCulture), monto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("3.5.0x")]
    public void Dinero_RechazaTextoQueNoEsMonto(string texto)
    {
        Assert.False(Dinero.TryParse(texto, out _));
    }

    [Fact]
    public void Dinero_LeeIgualConCualquierCulturaDeWindows()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            foreach (var codigo in new[] { "es-ES", "de-DE", "en-US", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(codigo);
                Assert.True(Dinero.TryParse("3.50", out var monto));
                Assert.Equal(3.50m, monto);
                Assert.Equal("3.50", Dinero.ParaEditar(3.5m));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(3.50, 350)]
    [InlineData(0.10, 10)]
    [InlineData(2.005, 201)] // redondeo comercial: la mitad sube
    public void Dinero_ConvierteACentavos(decimal monto, long centavos)
    {
        Assert.Equal(centavos, Dinero.ACentavos(monto));
        Assert.Equal(Dinero.Redondear(monto), Dinero.DesdeCentavos(centavos));
    }

    [Fact]
    public void Fechas_SeEscribenYLeenIgualConCualquierCultura()
    {
        var original = CultureInfo.CurrentCulture;
        var fecha = new DateTime(2026, 3, 9, 14, 5, 7);
        try
        {
            foreach (var codigo in new[] { "es-ES", "en-US", "fi-FI", "ar-SA" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(codigo);
                var texto = Fechas.ParaBaseDatos(fecha);
                Assert.Equal("2026-03-09 14:05:07", texto);
                Assert.Equal(fecha, Fechas.DesdeBaseDatos(texto));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Fechas_RangoCubreElDiaCompleto()
    {
        var dia = new DateTime(2026, 3, 9, 15, 30, 0);

        Assert.Equal("2026-03-09 00:00:00", Fechas.InicioDelDia(dia));
        Assert.Equal("2026-03-09 23:59:59", Fechas.FinDelDia(dia));
    }

    [Fact]
    public void Ticket_NingunaLineaExcedeElAnchoDelPapel()
    {
        var venta = new Venta
        {
            IdVenta = 42,
            FechaHora = new DateTime(2026, 3, 9, 14, 5, 7),
            MetodoPago = Venta.PagoEfectivo,
            Total = 1234.50m,
            MontoRecibido = 1300m,
            Cambio = 65.50m,
            Detalles =
            {
                new DetalleVenta { NombreProducto = "Queso duro viejo de la casa en presentación familiar extra grande", Cantidad = 12, PrecioUnitario = 99.95m },
                new DetalleVenta { NombreProducto = "Crema", Cantidad = 1, PrecioUnitario = 2.25m }
            }
        };

        var texto = TicketHelper.GenerarTexto(venta);
        var lineas = texto.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.All(lineas, l => Assert.True(l.Length <= ConfiguracionNegocio.AnchoTicket, $"Línea demasiado larga: \"{l}\""));
        Assert.Contains("Venta No: 42", texto);
        Assert.Contains("$1,234.50", texto);
        Assert.Contains("$65.50", texto);
    }
}
