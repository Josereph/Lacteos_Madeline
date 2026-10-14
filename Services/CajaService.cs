using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Services;

/// <summary>
/// Orquesta el control de caja: apertura, movimientos manuales,
/// cálculo de saldo y cierre. Evita aperturas duplicadas y cierres
/// sobre cajas inexistentes (sección 23 lineamientos).
/// El saldo esperado se calcula como:
///   MontoInicial + VentasEfectivo + TotalEntradas - TotalSalidas
/// Las ventas con tarjeta o transferencia no forman parte del efectivo.
/// </summary>
public class CajaService
{
    private readonly CajaRepository _cajaRepository = new();

    public (bool Exitoso, string Mensaje, Caja? CajaAbierta) AbrirCaja(decimal montoInicial)
    {
        // El fondo inicial puede ser cero, pero nunca negativo.
        var valMonto = Validaciones.ValidarMontoNoNegativo(montoInicial);
        if (!valMonto.EsValido)
        {
            return (false, valMonto.Mensaje, null);
        }

        try
        {
            if (_cajaRepository.ObtenerCajaAbierta() is not null)
            {
                return (false, "Ya existe una caja abierta. Debe cerrarla antes de abrir una nueva.", null);
            }

            var idCaja = _cajaRepository.AbrirCaja(Dinero.Redondear(montoInicial));
            return (true, "Caja abierta correctamente.", _cajaRepository.ObtenerPorId(idCaja));
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UX_Cajas_UnaAbierta
        {
            return (false, "Ya existe una caja abierta. Debe cerrarla antes de abrir una nueva.", null);
        }
        catch (Exception ex)
        {
            LogHelper.Error("Apertura de caja", ex);
            return (false, $"No se pudo abrir la caja: {ex.Message}", null);
        }
    }

    public Caja? ObtenerCajaAbierta()
    {
        return _cajaRepository.ObtenerCajaAbierta();
    }

    /// <summary>Todas las cajas, de la más reciente a la más antigua.</summary>
    public List<Caja> ObtenerHistorial()
    {
        return _cajaRepository.ObtenerHistorial();
    }

    public (bool Exitoso, string Mensaje) RegistrarMovimiento(int idCaja, string tipo, decimal monto, string? descripcion)
    {
        var valMonto = Validaciones.ValidarMonto(monto);
        if (!valMonto.EsValido)
        {
            return (false, valMonto.Mensaje);
        }

        if (tipo != "Entrada" && tipo != "Salida")
        {
            return (false, "El tipo de movimiento debe ser 'Entrada' o 'Salida'.");
        }

        if (Dinero.TieneMasDeDosDecimales(monto))
        {
            return (false, "El monto no puede tener más de dos decimales.");
        }

        try
        {
            // El repositorio valida caja abierta y efectivo suficiente
            // dentro de la misma transacción que inserta el movimiento.
            _cajaRepository.RegistrarMovimiento(new MovimientoCaja
            {
                IdCaja      = idCaja,
                FechaHora   = DateTime.Now,
                Tipo        = tipo,
                Monto       = monto,
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim()
            });

            return (true, "Movimiento registrado correctamente.");
        }
        catch (ReglaNegocioException ex)
        {
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Movimiento de caja #{idCaja} ({tipo} {monto})", ex);
            return (false, $"No se pudo registrar el movimiento: {ex.Message}");
        }
    }

    public ResumenCaja ObtenerResumen(int idCaja)
    {
        var caja = _cajaRepository.ObtenerPorId(idCaja)
            ?? throw new ReglaNegocioException("Caja no encontrada.");

        var totales = _cajaRepository.ObtenerTotales(idCaja)!;

        return new ResumenCaja
        {
            Caja               = caja,
            VentasEfectivo     = totales.VentasEfectivo,
            VentasOtrosMetodos = totales.VentasOtrosMetodos,
            TotalEntradas      = totales.Entradas,
            TotalSalidas       = totales.Salidas,
            SaldoEsperado      = totales.SaldoEsperado,
            Movimientos        = _cajaRepository.ObtenerMovimientos(idCaja)
        };
    }

    /// <summary>
    /// Cierra la caja: el saldo esperado se recalcula y se guarda junto con
    /// el monto contado y la diferencia en una sola transacción. Después se
    /// genera un respaldo automático de la base de datos; si el respaldo
    /// falla, el cierre sigue siendo válido y se informa el problema.
    /// </summary>
    public (bool Exitoso, string Mensaje, ResumenCaja? Resumen) CerrarCaja(int idCaja, decimal montoFinal)
    {
        var valMonto = Validaciones.ValidarMontoNoNegativo(montoFinal);
        if (!valMonto.EsValido)
        {
            return (false, "El monto contado no puede ser negativo.", null);
        }

        montoFinal = Dinero.Redondear(montoFinal);

        try
        {
            _cajaRepository.CerrarCaja(idCaja, montoFinal);
        }
        catch (ReglaNegocioException ex)
        {
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Cierre de caja #{idCaja}", ex);
            return (false, $"No se pudo cerrar la caja. No se guardó ningún cambio.\nDetalle: {ex.Message}", null);
        }

        var resumen = ObtenerResumen(idCaja);
        LogHelper.Info("Cierre de caja",
            $"Caja #{idCaja}: esperado {resumen.SaldoEsperado:F2}, contado {montoFinal:F2}, diferencia {resumen.Caja.Diferencia:F2}");

        var respaldo = BackupHelper.RealizarBackupAutomatico($"cierre-caja-{idCaja}");
        var mensaje = respaldo.Exitoso
            ? "Caja cerrada correctamente. Respaldo automático generado."
            : "Caja cerrada correctamente, pero NO se pudo generar el respaldo automático. Haga un respaldo manual.";

        return (true, mensaje, resumen);
    }
}

/// <summary>
/// DTO con el resumen financiero de una jornada de caja.
/// </summary>
public class ResumenCaja
{
    public Caja Caja { get; set; } = null!;
    public decimal VentasEfectivo { get; set; }
    public decimal VentasOtrosMetodos { get; set; }
    public decimal TotalVentas => VentasEfectivo + VentasOtrosMetodos;
    public decimal TotalEntradas { get; set; }
    public decimal TotalSalidas { get; set; }
    public decimal SaldoEsperado { get; set; }
    public List<MovimientoCaja> Movimientos { get; set; } = new();
}
