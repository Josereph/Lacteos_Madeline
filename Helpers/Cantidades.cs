using System.Globalization;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Cantidades de inventario (existencias, cantidades vendidas, compradas o
/// dadas de baja). En C# son <see cref="decimal"/> con hasta 3 decimales
/// (1.250 lb); en SQLite se guardan como milésimas enteras (1250), igual
/// que el dinero en centavos, para que las sumas sean exactas.
/// </summary>
public static class Cantidades
{
    public const int Decimales = 3;

    public static decimal Redondear(decimal cantidad) => Math.Round(cantidad, Decimales, MidpointRounding.AwayFromZero);

    public static long AMilesimas(decimal cantidad) => (long)(Redondear(cantidad) * 1000m);

    public static decimal DesdeMilesimas(long milesimas) => milesimas / 1000m;

    public static decimal LeerCantidad(this SqliteDataReader reader, int columna) =>
        DesdeMilesimas(reader.GetInt64(columna));

    public static bool TieneDemasiadosDecimales(decimal cantidad) => Redondear(cantidad) != cantidad;

    /// <summary>Acepta punto o coma decimal, igual que los montos.</summary>
    public static bool TryParse(string? texto, out decimal cantidad) => Dinero.TryParse(texto, out cantidad);

    /// <summary>Cantidad lista para un cuadro de texto editable ("1.25", "7").</summary>
    public static string ParaEditar(decimal cantidad) =>
        Redondear(cantidad).ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Cantidad con su unidad para mostrar: "7 u", "1.25 lb".</summary>
    public static string Formatear(decimal cantidad, string? unidad) =>
        $"{Redondear(cantidad).ToString("0.###", ConfiguracionNegocio.Cultura)} {UnidadMedida.Abreviatura(unidad)}";
}
