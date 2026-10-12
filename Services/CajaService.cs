using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

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

        var cajaExistente = _cajaRepository.ObtenerCajaAbierta();
        if (cajaExistente is not null)
        {
            return (false, "Ya existe una caja abierta. Debe cerrarla antes de abrir una nueva.", null);
        }

        var idCaja = _cajaRepository.AbrirCaja(montoInicial);
        var caja = _cajaRepository.ObtenerPorId(idCaja);

        return (true, "Caja abierta correctamente.", caja);
    }

    public Caja? ObtenerCajaAbierta()
    {
        return _cajaRepository.ObtenerCajaAbierta();
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

        var caja = _cajaRepository.ObtenerPorId(idCaja);
        if (caja is null || caja.Estado != "Abierta")
        {
            return (false, "No se encontró una caja abierta para registrar el movimiento.");
        }

        if (tipo == "Salida")
        {
            var saldoActual = ObtenerResumen(idCaja).SaldoEsperado;
            if (monto > saldoActual)
            {
                return (false, $"La salida ({monto:C2}) supera el efectivo disponible en caja ({saldoActual:C2}).");
            }
        }

        _cajaRepository.RegistrarMovimiento(new MovimientoCaja
        {
            IdCaja      = idCaja,
            FechaHora   = DateTime.Now,
            Tipo        = tipo,
            Monto       = monto,
            Descripcion = descripcion
        });

        return (true, "Movimiento registrado correctamente.");
    }

    public ResumenCaja ObtenerResumen(int idCaja)
    {
        var caja = _cajaRepository.ObtenerPorId(idCaja)
            ?? throw new InvalidOperationException("Caja no encontrada.");

        var movimientos = _cajaRepository.ObtenerMovimientos(idCaja);

        // Solo las ventas en efectivo suman al dinero físico de la caja;
        // tarjeta y transferencia se informan aparte.
        var (ventasEfectivo, ventasOtrosMetodos) = _cajaRepository.ObtenerTotalesVentas(idCaja);

        var totalEntradas = movimientos
            .Where(m => m.Tipo == "Entrada")
            .Sum(m => m.Monto);

        var totalSalidas = movimientos
            .Where(m => m.Tipo == "Salida")
            .Sum(m => m.Monto);

        var saldoEsperado = caja.MontoInicial + ventasEfectivo + totalEntradas - totalSalidas;

        return new ResumenCaja
        {
            Caja           = caja,
            VentasEfectivo = ventasEfectivo,
            VentasOtrosMetodos = ventasOtrosMetodos,
            TotalEntradas  = totalEntradas,
            TotalSalidas   = totalSalidas,
            SaldoEsperado  = saldoEsperado,
            Movimientos    = movimientos
        };
    }

    public (bool Exitoso, string Mensaje, ResumenCaja? Resumen) CerrarCaja(int idCaja, decimal montoFinal)
    {
        if (montoFinal < 0)
        {
            return (false, "El monto final no puede ser negativo.", null);
        }

        var caja = _cajaRepository.ObtenerPorId(idCaja);
        if (caja is null)
        {
            return (false, "No se encontró la caja indicada.", null);
        }

        if (caja.Estado != "Abierta")
        {
            return (false, "La caja ya se encuentra cerrada.", null);
        }

        _cajaRepository.CerrarCaja(idCaja, montoFinal);

        var resumen = ObtenerResumen(idCaja);

        return (true, "Caja cerrada correctamente.", resumen);
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
