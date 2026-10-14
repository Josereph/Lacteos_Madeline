using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Reglas del módulo de proveedores: nombre obligatorio y datos opcionales
/// sin espacios sobrantes. Los proveedores no se eliminan, se desactivan,
/// para conservar el historial de compras (sección 4 y 21 lineamientos).
/// </summary>
public class ProveedorService
{
    private readonly ProveedorRepository _proveedorRepository = new();

    public List<Proveedor> Buscar(string filtro = "", bool soloActivos = false)
    {
        return _proveedorRepository.Buscar(filtro, soloActivos);
    }

    /// <summary>
    /// Registra el proveedor si su Id es 0, o lo modifica si ya existe.
    /// </summary>
    public (bool Exitoso, string Mensaje) Guardar(Proveedor proveedor)
    {
        var validacion = Validaciones.ValidarTexto(proveedor.Nombre, "nombre del proveedor");
        if (!validacion.EsValido)
        {
            return validacion;
        }

        proveedor.Nombre = proveedor.Nombre.Trim();
        proveedor.Contacto = Limpiar(proveedor.Contacto);
        proveedor.Telefono = Limpiar(proveedor.Telefono);
        proveedor.Direccion = Limpiar(proveedor.Direccion);
        proveedor.Observaciones = Limpiar(proveedor.Observaciones);

        try
        {
            if (proveedor.IdProveedor == 0)
            {
                proveedor.IdProveedor = _proveedorRepository.Registrar(proveedor);
                return (true, "Proveedor registrado correctamente.");
            }

            _proveedorRepository.Modificar(proveedor);
            return (true, "Proveedor modificado correctamente.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Guardar proveedor \"{proveedor.Nombre}\"", ex);
            return (false, $"No se pudo guardar el proveedor.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    /// <summary>Activa el proveedor si está inactivo, o lo desactiva si está activo.</summary>
    public (bool Exitoso, string Mensaje) AlternarEstado(int idProveedor)
    {
        try
        {
            var proveedor = _proveedorRepository.ObtenerPorId(idProveedor);
            if (proveedor is null)
            {
                return (false, "El proveedor seleccionado ya no existe.");
            }

            _proveedorRepository.CambiarEstado(idProveedor, !proveedor.Estado);
            return (true, $"Proveedor {(proveedor.Estado ? "desactivado" : "activado")} correctamente.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Cambiar estado de proveedor #{idProveedor}", ex);
            return (false, $"No se pudo cambiar el estado.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
