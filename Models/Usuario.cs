using System.ComponentModel;

namespace LacteosMadeline.Models;

/// <summary>
/// Persona que usa el sistema. Entra eligiendo su nombre y escribiendo su
/// PIN (que se guarda cifrado, nunca en texto). El rol define qué módulos
/// ve: el cajero vende y maneja la caja; el administrador, todo lo demás.
/// </summary>
public class Usuario
{
    public const string RolAdministrador = "Administrador";
    public const string RolCajero = "Cajero";

    public static readonly string[] Roles = { RolAdministrador, RolCajero };

    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = RolCajero;
    public bool Estado { get; set; } = true;

    [Browsable(false)]
    public bool EsAdministrador => Rol == RolAdministrador;

    public override string ToString() => Nombre;
}
