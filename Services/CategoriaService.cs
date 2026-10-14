using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Reglas del módulo de categorías: nombre obligatorio y sin duplicados
/// (sin distinguir mayúsculas). Las categorías no se eliminan, solo se
/// desactivan, para no dejar productos sin clasificar.
/// </summary>
public class CategoriaService
{
    private readonly CategoriaRepository _categoriaRepository = new();

    public List<Categoria> ObtenerTodas(bool incluirInactivas = true)
    {
        return _categoriaRepository.ObtenerTodas(incluirInactivas);
    }

    /// <summary>
    /// Registra la categoría si <paramref name="idCategoria"/> es 0, o
    /// cambia su nombre si ya existe.
    /// </summary>
    public (bool Exitoso, string Mensaje) Guardar(int idCategoria, string? nombre)
    {
        var validacion = Validaciones.ValidarTexto(nombre, "nombre de la categoría");
        if (!validacion.EsValido)
        {
            return validacion;
        }

        nombre = nombre!.Trim();

        try
        {
            if (_categoriaRepository.ExisteNombre(nombre, idCategoria))
            {
                return (false, "Ya existe una categoría con ese nombre.");
            }

            if (idCategoria == 0)
            {
                _categoriaRepository.Registrar(new Categoria { Nombre = nombre, Estado = true });
                return (true, "Categoría registrada correctamente.");
            }

            var categoria = _categoriaRepository.ObtenerPorId(idCategoria);
            if (categoria is null)
            {
                return (false, "La categoría seleccionada ya no existe.");
            }

            categoria.Nombre = nombre;
            _categoriaRepository.Modificar(categoria);
            return (true, "Categoría modificada correctamente.");
        }
        catch (Exception ex) when (ErroresBaseDatos.EsReglaDeIntegridad(ex))
        {
            return (false, "Ya existe una categoría con ese nombre.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Guardar categoría \"{nombre}\"", ex);
            return (false, $"No se pudo guardar la categoría.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    /// <summary>Activa la categoría si está inactiva, o la desactiva si está activa.</summary>
    public (bool Exitoso, string Mensaje) AlternarEstado(int idCategoria)
    {
        try
        {
            var categoria = _categoriaRepository.ObtenerPorId(idCategoria);
            if (categoria is null)
            {
                return (false, "La categoría seleccionada ya no existe.");
            }

            _categoriaRepository.CambiarEstado(idCategoria, !categoria.Estado);
            return (true, $"Categoría {(categoria.Estado ? "desactivada" : "activada")} correctamente.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Cambiar estado de categoría #{idCategoria}", ex);
            return (false, $"No se pudo cambiar el estado.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }
}
