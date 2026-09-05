namespace LacteosMadeline.Models;

/// <summary>
/// Categoría utilizada para clasificar los productos (Quesos, Cremas, Leche, etc.).
/// </summary>
public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
}
