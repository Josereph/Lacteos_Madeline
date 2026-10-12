namespace LacteosMadeline.Helpers;

/// <summary>
/// Validaciones generales reutilizables por los distintos servicios y
/// formularios, para no repetir la misma lógica en cada uno (sección 14
/// de los lineamientos del proyecto).
/// </summary>
public static class Validaciones
{
    public static (bool EsValido, string Mensaje) ValidarTexto(string? valor, string nombreCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return (false, $"Debe ingresar el {nombreCampo}.");
        }

        return (true, string.Empty);
    }

    public static (bool EsValido, string Mensaje) ValidarPrecio(decimal precio, string nombreCampo = "precio")
    {
        if (precio <= 0)
        {
            return (false, $"El {nombreCampo} debe ser mayor que cero.");
        }

        return (true, string.Empty);
    }

    public static (bool EsValido, string Mensaje) ValidarCantidad(int cantidad)
    {
        if (cantidad <= 0)
        {
            return (false, "La cantidad debe ser mayor que cero.");
        }

        return (true, string.Empty);
    }

    public static (bool EsValido, string Mensaje) ValidarExistenciaSuficiente(int existenciaDisponible, int cantidadSolicitada)
    {
        if (cantidadSolicitada > existenciaDisponible)
        {
            return (false, "Existencia insuficiente.");
        }

        return (true, string.Empty);
    }

    public static (bool EsValido, string Mensaje) ValidarMonto(decimal monto)
    {
        if (monto <= 0)
        {
            return (false, "El monto debe ser mayor que cero.");
        }

        return (true, string.Empty);
    }

    public static (bool EsValido, string Mensaje) ValidarMontoNoNegativo(decimal monto)
    {
        if (monto < 0)
        {
            return (false, "El monto no puede ser negativo.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// RN-12: el efectivo recibido debe cubrir el total de la venta.
    /// </summary>
    public static (bool EsValido, string Mensaje) ValidarPagoEfectivo(decimal total, decimal? montoRecibido)
    {
        if (montoRecibido is null)
        {
            return (false, "Ingrese el monto recibido en efectivo.");
        }

        if (montoRecibido < total)
        {
            return (false, $"El monto recibido ({montoRecibido:C2}) es menor que el total ({total:C2}).");
        }

        return (true, string.Empty);
    }

    public static (bool EsValido, string Mensaje) ValidarCosto(decimal costo)
    {
        if (costo < 0)
        {
            return (false, "El costo no puede ser negativo.");
        }

        return (true, string.Empty);
    }
}
