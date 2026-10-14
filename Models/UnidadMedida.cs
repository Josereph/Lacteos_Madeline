namespace LacteosMadeline.Models;

/// <summary>
/// Unidades en que se vende un producto. "Unidad" se vende por piezas
/// enteras; las demás (por peso o volumen) aceptan cantidades con hasta
/// tres decimales, por ejemplo 1.250 lb de queso.
/// </summary>
public static class UnidadMedida
{
    public const string Unidad = "Unidad";
    public const string Libra = "Libra";
    public const string Kilogramo = "Kilogramo";
    public const string Litro = "Litro";

    public static readonly string[] Todas = { Unidad, Libra, Kilogramo, Litro };

    public static bool EsValida(string? unidad) => unidad is not null && Todas.Contains(unidad);

    public static bool PermiteDecimales(string? unidad) => unidad is not null && unidad != Unidad;

    public static string Abreviatura(string? unidad) => unidad switch
    {
        Libra => "lb",
        Kilogramo => "kg",
        Litro => "L",
        _ => "u"
    };
}
