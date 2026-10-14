using LacteosMadeline.Helpers;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Control de caja: apertura, registro de movimientos manuales,
/// resumen en tiempo real, cierre con diferencia, e historial de
/// cajas anteriores (secciones 10-14 de los lineamientos).
/// </summary>
public partial class FormCaja : Form
{
    private readonly CajaService _cajaService = new();
    private int _idCajaActual;

    public FormCaja()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        AgregarBotonCorte();
        Load += FormCaja_Load;
    }

    /// <summary>
    /// En el historial, un botón debajo de la tabla imprime el corte de la
    /// caja seleccionada (cerrada, o corte parcial si está abierta).
    /// </summary>
    private void AgregarBotonCorte()
    {
        var btnImprimirCorte = new Button
        {
            Name = "btnImprimirCorte",
            Text = "Ver / imprimir corte de la caja seleccionada",
            Size = new Size(330, 34),
            Location = new Point(0, 10)
        };
        btnImprimirCorte.Click += (_, _) =>
        {
            if (dgvHistorial.CurrentRow?.DataBoundItem is Models.Caja caja)
            {
                MostrarCorte(caja.IdCaja);
            }
            else
            {
                MostrarMensaje("Seleccione una caja del historial.");
            }
        };
        Tema.EstilizarBoton(btnImprimirCorte, Tema.RolBoton.Secundario);

        var pie = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        pie.Controls.Add(btnImprimirCorte);
        tabHistorial.Controls.Add(pie);
        dgvHistorial.BringToFront();
    }

    /// <summary>Muestra el corte de caja y ofrece imprimirlo.</summary>
    private void MostrarCorte(int idCaja)
    {
        var resumen = _cajaService.ObtenerResumen(idCaja);
        var texto = TicketHelper.GenerarCorteCaja(resumen);

        var respuesta = MessageBox.Show(this, $"{texto}\n¿Desea imprimir el corte?", $"Corte de la caja N.º {idCaja}",
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (respuesta != DialogResult.Yes) return;

        var impresion = TicketHelper.ImprimirTexto(texto, $"Corte de caja {idCaja}");
        if (!impresion.Exitoso)
        {
            MessageBox.Show(this, $"No se pudo imprimir el corte.\n\n{impresion.Mensaje}", "Impresión",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
            lblEstado.ForeColor = Tema.GrisCalido;
        }
        else
        {
            _idCajaActual = cajaAbierta.IdCaja;
            MostrarPanelApertura(visible: false);
            MostrarPanelOperaciones(visible: true);
            lblEstado.Text = $"Caja #{cajaAbierta.IdCaja} abierta desde {cajaAbierta.FechaApertura:dd/MM/yyyy HH:mm}";
            lblEstado.ForeColor = Tema.Exito;
            ActualizarResumen();
        }
    }

    // ── Apertura ────────────────────────────────────────────────────────

    private void btnAbrir_Click(object? sender, EventArgs e)
    {
        // Campo vacío = fondo inicial de cero.
        var textoMonto = txtMontoInicial.Text.Trim();
        decimal monto = 0;
        if (textoMonto.Length > 0 && !Dinero.TryParse(textoMonto, out monto))
        {
            MostrarMensaje("Ingrese un monto inicial válido (solo números).");
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

        if (!Dinero.TryParse(txtMonto.Text, out var monto))
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
                $"Resumen de cierre de la caja N.º {r.Caja.IdCaja}\n\n" +
                $"Fondo inicial: {r.Caja.MontoInicial:C2}\n" +
                $"Ventas en efectivo: {r.VentasEfectivo:C2}\n" +
                $"Entradas manuales: {r.TotalEntradas:C2}\n" +
                $"Salidas manuales: {r.TotalSalidas:C2}\n\n" +
                $"Efectivo esperado: {r.SaldoEsperado:C2}\n" +
                $"Efectivo contado: {montoFinal:C2}\n" +
                $"Diferencia: {diferencia:C2}\n\n" +
                $"Ventas con tarjeta o transferencia (no están en caja): {r.VentasOtrosMetodos:C2}\n" +
                $"Total vendido en la jornada: {r.TotalVentas:C2}  ({r.NumeroVentas} ventas)\n" +
                $"Descuentos: {r.TotalDescuentos:C2}  ·  Ventas anuladas: {r.VentasAnuladas}\n\n" +
                resultado.Mensaje,
                "Caja cerrada",
                MessageBoxButtons.OK,
                icono);

            RefrescarEstado();
            CargarHistorialCajas();
            MostrarCorte(r.Caja.IdCaja);
        }
    }

    // ── Resumen en tiempo real ──────────────────────────────────────────

    private void ActualizarResumen()
    {
        if (_idCajaActual == 0) return;

        var resumen = _cajaService.ObtenerResumen(_idCajaActual);

        lblMontoInicial.Text  = $"{resumen.Caja.MontoInicial:C2}";
        lblVentas.Text        = $"{resumen.VentasEfectivo:C2}";
        lblVentasOtros.Text   = $"Tarjeta / transferencia: {resumen.VentasOtrosMetodos:C2}\n(no suma al efectivo)\nTotal vendido: {resumen.TotalVentas:C2}";
        lblEntradas.Text      = $"{resumen.TotalEntradas:C2}";
        lblSalidas.Text       = $"{resumen.TotalSalidas:C2}";
        lblSaldoEsperado.Text = $"{resumen.SaldoEsperado:C2}";

        dgvMovimientos.DataSource = null;
        dgvMovimientos.DataSource = resumen.Movimientos;

        if (dgvMovimientos.Columns.Count > 0 && dgvMovimientos.Columns["IdMovimiento"] != null)
        {
            dgvMovimientos.OcultarColumna("IdMovimiento");
            dgvMovimientos.OcultarColumna("IdCaja");
            dgvMovimientos.Encabezado("FechaHora", "Fecha / Hora");
            dgvMovimientos.Encabezado("Tipo", "Tipo");
            dgvMovimientos.Encabezado("Monto", "Monto");
            dgvMovimientos.Encabezado("Descripcion", "Descripción");
            dgvMovimientos.Encabezado("NombreUsuario", "Usuario");
            dgvMovimientos.FormatoColumna("Monto", "C2");
        }
    }

    // ── Historial de cajas ──────────────────────────────────────────────

    private void CargarHistorialCajas()
    {
        var historial = _cajaService.ObtenerHistorial();

        dgvHistorial.DataSource = null;
        dgvHistorial.DataSource = historial;

        if (dgvHistorial.Columns.Count > 0 && dgvHistorial.Columns["IdCaja"] != null)
        {
            dgvHistorial.Encabezado("IdCaja", "# Caja");
            dgvHistorial.Encabezado("FechaApertura", "Apertura");
            dgvHistorial.Encabezado("MontoInicial", "Monto Inicial");
            dgvHistorial.Encabezado("FechaCierre", "Cierre");
            dgvHistorial.Encabezado("MontoFinal", "Monto Contado");
            dgvHistorial.Encabezado("MontoEsperado", "Monto Esperado");
            dgvHistorial.Encabezado("Diferencia", "Diferencia");
            dgvHistorial.Encabezado("Estado", "Estado");
            dgvHistorial.Encabezado("NombreUsuarioApertura", "Abrió");
            dgvHistorial.Encabezado("NombreUsuarioCierre", "Cerró");
            foreach (var columna in new[] { "MontoInicial", "MontoFinal", "MontoEsperado", "Diferencia" })
            {
                dgvHistorial.FormatoColumna(columna, "C2");
            }
        }

        // La lista de movimientos no se muestra en el historial; según el
        // enlace de datos la columna puede no generarse, por eso se verifica.
        if (dgvHistorial.Columns["Movimientos"] is { } columnaMovimientos)
        {
            columnaMovimientos.Visible = false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void MostrarPanelApertura(bool visible)   => panelApertura.Visible = visible;
    private void MostrarPanelOperaciones(bool visible) => panelOperaciones.Visible = visible;

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        lblMensaje.Text = mensaje;
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        // Primero los anclajes dentro de cada grupo, mientras los grupos
        // conservan su tamaño de diseño (el orden importa).
        Diseno.Anclar(Diseno.ArribaAncho, txtDescripcion);
        Diseno.Anclar(Diseno.ArribaDerecha, btnRegistrarMovimiento, btnCerrarCaja);

        Diseno.Anclar(Diseno.ArribaAncho, lblEstado);
        Diseno.Anclar(Diseno.Todo, panelOperaciones);
        Diseno.Anclar(Diseno.AbajoAncho, lblMensaje);
        tabCaja.Dock = DockStyle.Fill;

        // Dentro de las pestañas se usa Dock (las páginas aún no tienen su
        // tamaño real al construir el formulario, así que no sirven anclajes):
        // movimiento arriba, resumen abajo y la tabla ocupa el resto.
        tabJornada.Padding = new Padding(8);
        grpMovimiento.Dock = DockStyle.Top;
        grpResumen.Dock = DockStyle.Bottom;
        dgvMovimientos.Dock = DockStyle.Fill;
        dgvMovimientos.BringToFront();

        tabHistorial.Padding = new Padding(8);
        dgvHistorial.Dock = DockStyle.Fill;

    }
}
