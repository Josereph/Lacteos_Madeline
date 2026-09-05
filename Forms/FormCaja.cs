using LacteosMadeline.Repositories;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Control de caja: apertura, registro de movimientos manuales,
/// resumen en tiempo real, cierre con diferencia, e historial de
/// cajas anteriores (secciones 10-14 de los lineamientos).
/// </summary>
public partial class FormCaja : Form
{
    private readonly CajaService _cajaService = new();
    private readonly CajaRepository _cajaRepository = new();
    private int _idCajaActual;

    public FormCaja()
    {
        InitializeComponent();
        Load += FormCaja_Load;
    }

    private void FormCaja_Load(object? sender, EventArgs e)
    {
        RefrescarEstado();
        CargarHistorialCajas();
    }

    // ── Estado de caja ──────────────────────────────────────────────────

    private void RefrescarEstado()
    {
        var cajaAbierta = _cajaService.ObtenerCajaAbierta();

        if (cajaAbierta is null)
        {
            _idCajaActual = 0;
            MostrarPanelApertura(visible: true);
            MostrarPanelOperaciones(visible: false);
            lblEstado.Text = "Estado: Sin caja abierta";
            lblEstado.ForeColor = Color.Gray;
        }
        else
        {
            _idCajaActual = cajaAbierta.IdCaja;
            MostrarPanelApertura(visible: false);
            MostrarPanelOperaciones(visible: true);
            lblEstado.Text = $"Caja #{cajaAbierta.IdCaja} abierta desde {cajaAbierta.FechaApertura:dd/MM/yyyy HH:mm}";
            lblEstado.ForeColor = Color.DarkGreen;
            ActualizarResumen();
        }
    }

    // ── Apertura ────────────────────────────────────────────────────────

    private void btnAbrir_Click(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(txtMontoInicial.Text.Trim(), out var monto))
        {
            MostrarMensaje("Ingrese un monto inicial válido.");
            return;
        }

        var resultado = _cajaService.AbrirCaja(monto);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            txtMontoInicial.Clear();
            RefrescarEstado();
            CargarHistorialCajas();
        }
    }

    // ── Movimientos ─────────────────────────────────────────────────────

    private void btnRegistrarMovimiento_Click(object? sender, EventArgs e)
    {
        if (_idCajaActual == 0)
        {
            MostrarMensaje("No hay una caja abierta.");
            return;
        }

        var tipo = rbEntrada.Checked ? "Entrada" : "Salida";

        if (!decimal.TryParse(txtMonto.Text.Trim(), out var monto))
        {
            MostrarMensaje("Ingrese un monto válido.");
            return;
        }

        var descripcion = txtDescripcion.Text.Trim();
        if (string.IsNullOrEmpty(descripcion))
        {
            MostrarMensaje("Ingrese una descripción para el movimiento.");
            return;
        }

        var resultado = _cajaService.RegistrarMovimiento(_idCajaActual, tipo, monto, descripcion);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            txtMonto.Clear();
            txtDescripcion.Clear();
            ActualizarResumen();
        }
    }

    // ── Cierre ──────────────────────────────────────────────────────────

    private void btnCerrarCaja_Click(object? sender, EventArgs e)
    {
        if (_idCajaActual == 0)
        {
            MostrarMensaje("No hay una caja abierta para cerrar.");
            return;
        }

        // Calcular saldo esperado antes de pedir el contado
        var resumenActual = _cajaService.ObtenerResumen(_idCajaActual);

        using var dlg = new FormInputMonto(
            "Cierre de caja",
            $"Saldo esperado: {resumenActual.SaldoEsperado:C2}\nIngrese el monto contado en efectivo:",
            resumenActual.SaldoEsperado);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var montoFinal = dlg.Monto;

        var resultado = _cajaService.CerrarCaja(_idCajaActual, montoFinal);
        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso && resultado.Resumen is not null)
        {
            var r = resultado.Resumen;
            var diferencia = montoFinal - r.SaldoEsperado;
            var icono = diferencia == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;

            MessageBox.Show(
                $"RESUMEN DE CIERRE – Caja #{r.Caja.IdCaja}\n" +
                $"────────────────────────────────\n" +
                $"Monto inicial:        {r.Caja.MontoInicial:C2}\n" +
                $"Ventas:              +{r.TotalVentas:C2}\n" +
                $"Entradas manuales:   +{r.TotalEntradas:C2}\n" +
                $"Salidas manuales:    -{r.TotalSalidas:C2}\n" +
                $"────────────────────────────────\n" +
                $"Saldo esperado:       {r.SaldoEsperado:C2}\n" +
                $"Monto contado:        {montoFinal:C2}\n" +
                $"Diferencia:           {diferencia:C2}",
                "Caja cerrada",
                MessageBoxButtons.OK,
                icono);

            RefrescarEstado();
            CargarHistorialCajas();
        }
    }

    // ── Resumen en tiempo real ──────────────────────────────────────────

    private void ActualizarResumen()
    {
        if (_idCajaActual == 0) return;

        var resumen = _cajaService.ObtenerResumen(_idCajaActual);

        lblMontoInicial.Text  = $"Monto inicial:        {resumen.Caja.MontoInicial:C2}";
        lblVentas.Text        = $"Ventas:              +{resumen.TotalVentas:C2}";
        lblEntradas.Text      = $"Entradas:            +{resumen.TotalEntradas:C2}";
        lblSalidas.Text       = $"Salidas:             -{resumen.TotalSalidas:C2}";
        lblSaldoEsperado.Text = $"Saldo esperado:       {resumen.SaldoEsperado:C2}";

        dgvMovimientos.DataSource = null;
        dgvMovimientos.DataSource = resumen.Movimientos;

        if (dgvMovimientos.Columns.Count > 0 && dgvMovimientos.Columns["IdMovimiento"] != null)
        {
            dgvMovimientos.Columns["IdMovimiento"].Visible = false;
            dgvMovimientos.Columns["IdCaja"].Visible = false;
            dgvMovimientos.Columns["FechaHora"].HeaderText = "Fecha / Hora";
            dgvMovimientos.Columns["Tipo"].HeaderText = "Tipo";
            dgvMovimientos.Columns["Monto"].HeaderText = "Monto";
            dgvMovimientos.Columns["Descripcion"].HeaderText = "Descripción";
        }
    }

    // ── Historial de cajas ──────────────────────────────────────────────

    private void CargarHistorialCajas()
    {
        var historial = _cajaRepository.ObtenerHistorial();

        dgvHistorial.DataSource = null;
        dgvHistorial.DataSource = historial;

        if (dgvHistorial.Columns.Count > 0 && dgvHistorial.Columns["IdCaja"] != null)
        {
            dgvHistorial.Columns["IdCaja"].HeaderText = "# Caja";
            dgvHistorial.Columns["FechaApertura"].HeaderText = "Apertura";
            dgvHistorial.Columns["MontoInicial"].HeaderText = "Monto Inicial";
            dgvHistorial.Columns["FechaCierre"].HeaderText = "Cierre";
            dgvHistorial.Columns["MontoFinal"].HeaderText = "Monto Final";
            dgvHistorial.Columns["Estado"].HeaderText = "Estado";
            dgvHistorial.Columns["Movimientos"].Visible = false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void MostrarPanelApertura(bool visible)   => panelApertura.Visible = visible;
    private void MostrarPanelOperaciones(bool visible) => panelOperaciones.Visible = visible;

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        lblMensaje.Text = mensaje;
    }
}
