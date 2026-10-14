using System.Globalization;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Conversión de montos entre la aplicación y la base de datos.
/// En C# los montos son <see cref="decimal"/>; en SQLite se guardan como
/// centavos enteros (INTEGER), que se suman sin residuos de punto flotante.
/// Toda lectura o escritura de un monto debe pasar por esta clase.
/// </summary>
public static class Dinero
{
    public static decimal Redondear(decimal monto) => Math.Round(monto, 2, MidpointRounding.AwayFromZero);

    public static long ACentavos(decimal monto) => (long)(Redondear(monto) * 100m);

    public static object ACentavos(decimal? monto) => monto is decimal valor ? ACentavos(valor) : DBNull.Value;

    public static decimal DesdeCentavos(long centavos) => centavos / 100m;

    public static decimal LeerDinero(this SqliteDataReader reader, int columna) =>
        DesdeCentavos(reader.GetInt64(columna));

    public static decimal? LeerDineroONulo(this SqliteDataReader reader, int columna) =>
        reader.IsDBNull(columna) ? null : DesdeCentavos(reader.GetInt64(columna));

    public static bool TieneMasDeDosDecimales(decimal monto) => Redondear(monto) != monto;

    /// <summary>
    /// Interpreta un monto escrito por el usuario sin depender de la
    /// configuración regional de Windows: acepta punto o coma como separador
    /// decimal ("3.50", "3,50") y el símbolo de moneda. Si aparecen ambos
    /// separadores, el último es el decimal ("1,250.50" o "1.250,50").
    /// </summary>
    public static bool TryParse(string? texto, out decimal monto)
    {
        monto = 0m;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Trim()
            .Replace(ConfiguracionNegocio.SimboloMoneda, string.Empty)
            .Replace(" ", string.Empty);

        var ultimaComa = limpio.LastIndexOf(',');
        var ultimoPunto = limpio.LastIndexOf('.');
        if (ultimaComa >= 0 && ultimoPunto >= 0)
        {
            var separadorMiles = ultimaComa > ultimoPunto ? "." : ",";
            limpio = limpio.Replace(separadorMiles, string.Empty);
        }

        limpio = limpio.Replace(',', '.');

        return decimal.TryParse(limpio, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out monto);
    }

    /// <summary>
    /// Monto listo para un cuadro de texto editable ("3.50"), sin símbolo
    /// de moneda, de forma que <see cref="TryParse"/> lo lea igual.
    /// </summary>
    public static string ParaEditar(decimal monto) => monto.ToString("0.00", CultureInfo.InvariantCulture);
}
