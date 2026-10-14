namespace LacteosMadeline.Models;

/// <summary>
/// Cliente al que se puede asociar una venta (opcional: sin cliente es
/// "consumidor final"). Se desactiva en lugar de eliminarse para conservar
/// el historial de sus compras.
/// </summary>
public class Cliente
{
    public const string ConsumidorFinal = "Consumidor final";

    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Correo { get; set; }
    public bool Estado { get; set; } = true;

    public override string ToString() => Nombre;
}
