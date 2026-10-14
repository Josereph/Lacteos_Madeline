using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

namespace LacteosMadeline.Services;

/// <summary>
/// Catálogo de clientes: nombre obligatorio, datos opcionales sin espacios
/// sobrantes. Los clientes se desactivan, no se eliminan, para conservar
/// el historial de sus compras.
/// </summary>
public class ClienteService
{
    private readonly ClienteRepository _clienteRepository = new();

    public List<Cliente> Buscar(string filtro = "", bool soloActivos = false)
    {
        return _clienteRepository.Buscar(filtro, soloActivos);
    }

    public Cliente? ObtenerPorId(int idCliente)
    {
        return _clienteRepository.ObtenerPorId(idCliente);
    }

    /// <summary>
    /// Registra el cliente si su Id es 0, o lo modifica si ya existe.
    /// Devuelve el Id guardado.
    /// </summary>
    public (bool Exitoso, string Mensaje, int IdCliente) Guardar(Cliente cliente)
    {
        var validacion = Validaciones.ValidarTexto(cliente.Nombre, "nombre del cliente");
        if (!validacion.EsValido)
        {
            return (false, validacion.Mensaje, 0);
        }

        cliente.Nombre = cliente.Nombre.Trim();
        cliente.Documento = Limpiar(cliente.Documento);
        cliente.Telefono = Limpiar(cliente.Telefono);
        cliente.Direccion = Limpiar(cliente.Direccion);
        cliente.Correo = Limpiar(cliente.Correo);

        if (cliente.Correo is not null && (!cliente.Correo.Contains('@') || cliente.Correo.Contains(' ')))
        {
            return (false, "El correo electrónico no es válido.", 0);
        }

        try
        {
            if (cliente.IdCliente == 0)
            {
                cliente.IdCliente = _clienteRepository.Registrar(cliente);
                return (true, "Cliente registrado correctamente.", cliente.IdCliente);
            }

            _clienteRepository.Modificar(cliente);
            return (true, "Cliente modificado correctamente.", cliente.IdCliente);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Guardar cliente \"{cliente.Nombre}\"", ex);
            return (false, $"No se pudo guardar el cliente.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}", 0);
        }
    }

    public (bool Exitoso, string Mensaje) AlternarEstado(int idCliente)
    {
        try
        {
            var cliente = _clienteRepository.ObtenerPorId(idCliente);
            if (cliente is null)
            {
                return (false, "El cliente seleccionado ya no existe.");
            }

            _clienteRepository.CambiarEstado(idCliente, !cliente.Estado);
            return (true, $"Cliente {(cliente.Estado ? "desactivado" : "activado")} correctamente.");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Cambiar estado de cliente #{idCliente}", ex);
            return (false, $"No se pudo cambiar el estado.\nDetalle: {ErroresBaseDatos.MensajeAmigable(ex)}");
        }
    }

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
