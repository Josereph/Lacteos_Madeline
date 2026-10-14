using LacteosMadeline.Models;

namespace LacteosMadeline.Services;

/// <summary>
/// Usuario que inició sesión en este equipo. Los servicios lo usan para
/// registrar quién hizo cada operación y para exigir un administrador en
/// las acciones delicadas (anular ventas, aplicar descuentos).
/// Sin sesión (pruebas automatizadas) las operaciones quedan sin usuario.
/// </summary>
public static class Sesion
{
    public static Usuario? Actual { get; private set; }

    public static int? IdUsuario => Actual?.IdUsuario;

    public static bool EsAdministrador => Actual?.EsAdministrador == true;

    public static void Iniciar(Usuario usuario) => Actual = usuario;

    public static void Cerrar() => Actual = null;

    /// <summary>
    /// Una acción reservada a administradores puede hacerla el usuario
    /// actual si es administrador, o un cajero con la autorización (PIN)
    /// de un administrador (<paramref name="idAutoriza"/>).
    /// </summary>
    public static bool PuedeHacerAccionDeAdministrador(int? idAutoriza) =>
        Actual is null || Actual.EsAdministrador || idAutoriza is not null;
}
