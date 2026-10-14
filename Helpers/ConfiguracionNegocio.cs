using System.Globalization;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Datos del negocio y formato regional. Los datos del ticket y la carpeta
/// de respaldo externa se editan desde Administración → Configuración y se
/// guardan en la base de datos (tabla Configuracion); aquí están sus valores
/// predeterminados. La moneda y la cultura son fijas en el código.
/// </summary>
public static class ConfiguracionNegocio
{
    // Claves en la tabla Configuracion.
    public const string ClaveNombreNegocio = "NombreNegocio";
    public const string ClaveDireccion = "Direccion";
    public const string ClaveTelefono = "Telefono";
    public const string ClaveDocumento = "Documento";
    public const string ClaveMensajeTicket = "MensajeTicket";
    public const string ClaveAnchoTicket = "AnchoTicket";
    public const string ClaveCarpetaRespaldoExterna = "CarpetaRespaldoExterna";

    // Valores predeterminados.
    public const string NombreNegocioPredeterminado = "LACTEOS MADELINE";
    public const string MensajeTicketPredeterminado = "Gracias por su compra.";
    public const int AnchoTicketPredeterminado = 32;

    /// <summary>Anchos de ticket admitidos: 32 (papel de 58 mm), 42 y 48 (papel de 80 mm).</summary>
    public static readonly int[] AnchosTicket = { 32, 42, 48 };

    // ── Datos impresos en el ticket ─────────────────────────────────────
    public static string NombreNegocio { get; private set; } = NombreNegocioPredeterminado;
    public static string Direccion { get; private set; } = string.Empty;   // vacío = no se imprime
    public static string Telefono { get; private set; } = string.Empty;    // vacío = no se imprime
    public static string Documento { get; private set; } = string.Empty;   // NIT / NRC; vacío = no se imprime
    public static string MensajeTicket { get; private set; } = MensajeTicketPredeterminado;

    /// <summary>Caracteres por línea del ticket.</summary>
    public static int AnchoTicket { get; private set; } = AnchoTicketPredeterminado;

    /// <summary>
    /// Carpeta adicional (USB, OneDrive…) donde se copia cada respaldo
    /// automático. Vacío = no se copia.
    /// </summary>
    public static string CarpetaRespaldoExterna { get; private set; } = string.Empty;

    /// <summary>
    /// Toma los valores guardados; las claves que falten o sean inválidas
    /// quedan con su valor predeterminado.
    /// </summary>
    public static void Aplicar(IReadOnlyDictionary<string, string> valores)
    {
        string Texto(string clave, string predeterminado) =>
            valores.TryGetValue(clave, out var valor) ? valor.Trim() : predeterminado;

        var nombre = Texto(ClaveNombreNegocio, NombreNegocioPredeterminado);
        NombreNegocio = nombre.Length == 0 ? NombreNegocioPredeterminado : nombre;
        Direccion = Texto(ClaveDireccion, string.Empty);
        Telefono = Texto(ClaveTelefono, string.Empty);
        Documento = Texto(ClaveDocumento, string.Empty);
        MensajeTicket = Texto(ClaveMensajeTicket, MensajeTicketPredeterminado);
        CarpetaRespaldoExterna = Texto(ClaveCarpetaRespaldoExterna, string.Empty);
        AnchoTicket = int.TryParse(Texto(ClaveAnchoTicket, string.Empty), out var ancho) && AnchosTicket.Contains(ancho)
            ? ancho
            : AnchoTicketPredeterminado;
    }

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
