using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Usuarios del sistema: alta, inicio de sesión con PIN, cambio de PIN,
/// rol y estado. Siempre debe quedar al menos un administrador activo, para
/// que nadie quede bloqueado fuera de la administración del sistema.
/// </summary>
public class UsuarioService
{
    private readonly UsuarioRepository _usuarioRepository = new();

    /// <summary>Si no hay ningún usuario, el programa pide crear el primer administrador.</summary>
    public bool RequiereConfiguracionInicial() => !_usuarioRepository.ExisteAlguno();

    public List<Usuario> ListarActivos() => _usuarioRepository.Listar(soloActivos: true);

    public List<Usuario> ListarTodos() => _usuarioRepository.Listar(soloActivos: false);

    public List<Usuario> ListarAdministradoresActivos() =>
        _usuarioRepository.Listar(soloActivos: true).Where(u => u.EsAdministrador).ToList();

    /// <summary>Primer usuario del sistema: siempre administrador.</summary>
    public (bool Exitoso, string Mensaje, Usuario? Usuario) CrearPrimerAdministrador(string? nombre, string? pin, string? confirmacion)
    {
        if (!RequiereConfiguracionInicial())
        {
            return (false, "Ya existen usuarios en el sistema.", null);
        }

        var resultado = Crear(nombre, Usuario.RolAdministrador, pin, confirmacion);
        return resultado.Exitoso
            ? (true, resultado.Mensaje, _usuarioRepository.ObtenerPorId(resultado.IdUsuario))
            : (false, resultado.Mensaje, null);
    }

    public (bool Exitoso, string Mensaje, int IdUsuario) Crear(string? nombre, string rol, string? pin, string? confirmacion)
    {
        var validacion = ValidarDatos(0, nombre, rol);
        if (!validacion.EsValido)
        {
            return (false, validacion.Mensaje, 0);
        }

        var validacionPin = ValidarPinNuevo(pin, confirmacion);
        if (!validacionPin.EsValido)
        {
            return (false, validacionPin.Mensaje, 0);
        }

        try
        {
            var id = _usuarioRepository.Registrar(new Usuario { Nombre = nombre!.Trim(), Rol = rol, Estado = true }, pin!);
            LogHelper.Info("Usuarios", $"Usuario creado: {nombre.Trim()} ({rol})");
            return (true, "Usuario creado correctamente.", id);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Crear usuario \"{nombre}\"", ex);
            return (false, $"No se pudo crear el usuario.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}", 0);
        }
    }

    /// <summary>Cambia nombre y rol. No permite dejar el sistema sin administradores.</summary>
    public (bool Exitoso, string Mensaje) Modificar(int idUsuario, string? nombre, string rol)
    {
        var validacion = ValidarDatos(idUsuario, nombre, rol);
        if (!validacion.EsValido)
        {
            return validacion;
        }

        try
        {
            var usuario = _usuarioRepository.ObtenerPorId(idUsuario);
            if (usuario is null)
            {
                return (false, "El usuario ya no existe.");
            }

            if (usuario.EsAdministrador && usuario.Estado && rol != Usuario.RolAdministrador
                && _usuarioRepository.ContarAdministradoresActivos(idUsuario) == 0)
            {
                return (false, "Debe quedar al menos un administrador activo.");
            }

            usuario.Nombre = nombre!.Trim();
            usuario.Rol = rol;
            _usuarioRepository.Modificar(usuario);
            return (true, "Usuario modificado correctamente.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Modificar usuario #{idUsuario}", ex);
            return (false, $"No se pudo modificar el usuario.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    public (bool Exitoso, string Mensaje) AlternarEstado(int idUsuario)
    {
        try
        {
            var usuario = _usuarioRepository.ObtenerPorId(idUsuario);
            if (usuario is null)
            {
                return (false, "El usuario ya no existe.");
            }

            if (usuario.Estado && usuario.EsAdministrador && _usuarioRepository.ContarAdministradoresActivos(idUsuario) == 0)
            {
                return (false, "No se puede desactivar al único administrador activo.");
            }

            if (usuario.Estado && usuario.IdUsuario == Sesion.IdUsuario)
            {
                return (false, "No puede desactivar su propio usuario mientras tiene la sesión iniciada.");
            }

            usuario.Estado = !usuario.Estado;
            _usuarioRepository.Modificar(usuario);
            return (true, $"Usuario {(usuario.Estado ? "activado" : "desactivado")} correctamente.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Cambiar estado de usuario #{idUsuario}", ex);
            return (false, $"No se pudo cambiar el estado.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    /// <summary>Un administrador asigna un PIN nuevo a cualquier usuario (PIN olvidado).</summary>
    public (bool Exitoso, string Mensaje) RestablecerPin(int idUsuario, string? pin, string? confirmacion)
    {
        var validacionPin = ValidarPinNuevo(pin, confirmacion);
        if (!validacionPin.EsValido)
        {
            return validacionPin;
        }

        if (_usuarioRepository.ObtenerPorId(idUsuario) is null)
        {
            return (false, "El usuario ya no existe.");
        }

        _usuarioRepository.CambiarPin(idUsuario, pin!);
        LogHelper.Info("Usuarios", $"PIN restablecido para el usuario #{idUsuario}");
        return (true, "PIN actualizado correctamente.");
    }

    /// <summary>Cada usuario cambia su propio PIN indicando el actual.</summary>
    public (bool Exitoso, string Mensaje) CambiarPropioPin(int idUsuario, string? pinActual, string? pinNuevo, string? confirmacion)
    {
        if (string.IsNullOrEmpty(pinActual) || _usuarioRepository.Verificar(idUsuario, pinActual) is null)
        {
            return (false, "El PIN actual no es correcto.");
        }

        if (pinActual == pinNuevo)
        {
            return (false, "El PIN nuevo debe ser distinto del actual.");
        }

        return RestablecerPin(idUsuario, pinNuevo, confirmacion);
    }

    /// <summary>Inicio de sesión: usuario activo y PIN correcto.</summary>
    public (bool Exitoso, string Mensaje, Usuario? Usuario) Autenticar(int idUsuario, string? pin)
    {
        if (string.IsNullOrEmpty(pin))
        {
            return (false, "Escriba su PIN.", null);
        }

        try
        {
            var usuario = _usuarioRepository.Verificar(idUsuario, pin);
            if (usuario is null)
            {
                LogHelper.Advertencia("Inicio de sesión", $"PIN incorrecto para el usuario #{idUsuario}");
                return (false, "PIN incorrecto.", null);
            }

            LogHelper.Info("Inicio de sesión", $"{usuario.Nombre} ({usuario.Rol})");
            return (true, string.Empty, usuario);
        }
        catch (Exception ex)
        {
            LogHelper.Error("Inicio de sesión", ex);
            return (false, $"No se pudo verificar el PIN.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}", null);
        }
    }

    /// <summary>Autorización de un administrador para una acción de un cajero.</summary>
    public (bool Exitoso, string Mensaje, Usuario? Administrador) Autorizar(int idAdministrador, string? pin)
    {
        var resultado = Autenticar(idAdministrador, pin);
        if (!resultado.Exitoso || resultado.Usuario is null)
        {
            return (false, resultado.Mensaje, null);
        }

        return resultado.Usuario.EsAdministrador
            ? (true, string.Empty, resultado.Usuario)
            : (false, "Solo un administrador puede autorizar esta acción.", null);
    }

    private (bool EsValido, string Mensaje) ValidarDatos(int idUsuario, string? nombre, string rol)
    {
        var validacionNombre = Validaciones.ValidarTexto(nombre, "nombre del usuario");
        if (!validacionNombre.EsValido)
        {
            return validacionNombre;
        }

        if (nombre!.Trim().Length > 40)
        {
            return (false, "El nombre del usuario es demasiado largo (máximo 40 caracteres).");
        }

        if (!Usuario.Roles.Contains(rol))
        {
            return (false, "Seleccione un rol válido.");
        }

        if (_usuarioRepository.ExisteNombre(nombre.Trim(), idUsuario))
        {
            return (false, "Ya existe un usuario con ese nombre.");
        }

        return (true, string.Empty);
    }

    private static (bool EsValido, string Mensaje) ValidarPinNuevo(string? pin, string? confirmacion)
    {
        var validacion = Pin.Validar(pin);
        if (!validacion.EsValido)
        {
            return validacion;
        }

        return pin == confirmacion ? (true, string.Empty) : (false, "La confirmación no coincide con el PIN.");
    }
}
