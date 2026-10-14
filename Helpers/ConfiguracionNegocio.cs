using System.Globalization;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Datos del negocio y formato regional, en un solo lugar. Para adaptar el
/// sistema a otro negocio basta con cambiar estos valores.
/// </summary>
public static class ConfiguracionNegocio
{
    // ── Datos impresos en el ticket ─────────────────────────────────────
    public const string NombreNegocio = "LACTEOS MADELINE";
    public const string Direccion = "";   // vacío = no se imprime
    public const string Telefono = "";    // vacío = no se imprime
    public const string MensajeTicket = "Gracias por su compra.";

    /// <summary>
    /// Caracteres por línea del ticket: 32 para papel de 58 mm,
    /// 42 o 48 para papel de 80 mm.
    /// </summary>
    public const int AnchoTicket = 32;

    // ── Formato regional ────────────────────────────────────────────────
    public const string CodigoCultura = "es-SV";
    public const string SimboloMoneda = "$";

    /// <summary>
    /// Cultura de toda la aplicación: idioma español, moneda con
    /// <see cref="SimboloMoneda"/>, punto decimal y coma de miles. Se fija
    /// aquí para que montos y fechas se vean igual en cualquier PC, sin
    /// depender de la configuración regional de Windows.
    /// </summary>
    public static CultureInfo Cultura { get; } = CrearCultura();

    private static CultureInfo CrearCultura()
    {
        CultureInfo baseCultura;
        try
        {
            baseCultura = CultureInfo.GetCultureInfo(CodigoCultura);
        }
        catch (CultureNotFoundException)
        {
            baseCultura = CultureInfo.GetCultureInfo("es");
        }

        var cultura = (CultureInfo)baseCultura.Clone();
        var numeros = cultura.NumberFormat;
        numeros.CurrencySymbol = SimboloMoneda;
        numeros.CurrencyDecimalSeparator = ".";
        numeros.CurrencyGroupSeparator = ",";
        numeros.CurrencyPositivePattern = 0; // $n
        numeros.CurrencyNegativePattern = 1; // -$n
        numeros.NumberDecimalSeparator = ".";
        numeros.NumberGroupSeparator = ",";
        numeros.PercentDecimalSeparator = ".";
        numeros.PercentGroupSeparator = ",";
        return CultureInfo.ReadOnly(cultura);
    }

    /// <summary>
    /// Aplica <see cref="Cultura"/> al hilo actual y a los que se creen después.
    /// </summary>
    public static void AplicarCultura()
    {
        CultureInfo.DefaultThreadCurrentCulture = Cultura;
        CultureInfo.DefaultThreadCurrentUICulture = Cultura;
        CultureInfo.CurrentCulture = Cultura;
        CultureInfo.CurrentUICulture = Cultura;
    }
}
