using LacteosMadeline.Helpers;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Lee y guarda la configuración editable del negocio (datos del ticket,
/// carpeta de respaldo externa) y la aplica a <see cref="ConfiguracionNegocio"/>.
/// </summary>
public class ConfiguracionService
{
    private readonly ConfiguracionRepository _configuracionRepository = new();

    /// <summary>Carga la configuración guardada (o los valores predeterminados).</summary>
    public void Cargar()
    {
        try
        {
            ConfiguracionNegocio.Aplicar(_configuracionRepository.ObtenerTodo());
        }
        catch (Exception ex)
        {
            // Sin configuración el sistema sigue funcionando con los valores predeterminados.
            LogHelper.Error("Cargar configuración", ex);
            ConfiguracionNegocio.Aplicar(new Dictionary<string, string>());
        }
    }

    public (bool Exitoso, string Mensaje) GuardarDatosNegocio(
        string? nombre, string? direccion, string? telefono, string? documento, string? mensajeTicket, int anchoTicket)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return (false, "El nombre del negocio es obligatorio.");
        }

        if (!ConfiguracionNegocio.AnchosTicket.Contains(anchoTicket))
        {
            return (false, "Seleccione un ancho de ticket válido.");
        }

        return Guardar(new Dictionary<string, string>
        {
            [ConfiguracionNegocio.ClaveNombreNegocio] = nombre.Trim(),
            [ConfiguracionNegocio.ClaveDireccion] = direccion?.Trim() ?? string.Empty,
            [ConfiguracionNegocio.ClaveTelefono] = telefono?.Trim() ?? string.Empty,
            [ConfiguracionNegocio.ClaveDocumento] = documento?.Trim() ?? string.Empty,
            [ConfiguracionNegocio.ClaveMensajeTicket] = mensajeTicket?.Trim() ?? string.Empty,
            [ConfiguracionNegocio.ClaveAnchoTicket] = anchoTicket.ToString()
        }, "Datos del negocio guardados.");
    }

    /// <summary>
    /// Carpeta adicional para los respaldos automáticos (USB, OneDrive…).
    /// Vacío la desactiva. No puede ser la propia carpeta de datos.
    /// </summary>
    public (bool Exitoso, string Mensaje) GuardarCarpetaRespaldoExterna(string? carpeta)
    {
        carpeta = carpeta?.Trim() ?? string.Empty;

        if (carpeta.Length > 0)
        {
            string completa;
            try
            {
                completa = Path.GetFullPath(carpeta);
            }
            catch (Exception)
            {
                return (false, "La ruta de la carpeta no es válida.");
            }

            var datos = Path.GetFullPath(Data.DatabaseConnection.DatabaseFolder);
            if (completa.StartsWith(datos, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "La copia externa debe estar fuera de la carpeta de datos del programa (por ejemplo, en una USB).");
            }

            if (!Directory.Exists(completa))
            {
                return (false, "La carpeta no existe o no está conectada.");
            }

            carpeta = completa;
        }

        return Guardar(new Dictionary<string, string> { [ConfiguracionNegocio.ClaveCarpetaRespaldoExterna] = carpeta },
            carpeta.Length == 0 ? "Copia externa desactivada." : $"Cada respaldo automático se copiará también en:\n{carpeta}");
    }

    private (bool Exitoso, string Mensaje) Guardar(Dictionary<string, string> valores, string mensajeExito)
    {
        try
        {
            _configuracionRepository.Guardar(valores);
            Cargar();
            return (true, mensajeExito);
        }
        catch (Exception ex)
        {
            LogHelper.Error("Guardar configuración", ex);
            return (false, $"No se pudo guardar la configuración.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }
}
