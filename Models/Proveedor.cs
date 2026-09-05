namespace LacteosMadeline.Models;

/// <summary>
/// Proveedor que abastece a Lácteos Madeline. Se prefiere desactivar
/// (Estado = false) antes que eliminar físicamente cuando ya tiene
/// compras asociadas, para conservar el historial (sección 4 lineamientos).
/// </summary>
public class Proveedor
{
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Observaciones { get; set; }
    public bool Estado { get; set; } = true;
}
