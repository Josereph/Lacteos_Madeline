using System.Diagnostics;
using LacteosMadeline.Data;
using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Helpers;
using LacteosMadeline.Services;

namespace LacteosMadeline.Forms;

/// <summary>
/// Configuración y respaldos: datos del negocio que se imprimen en el
/// ticket, carpeta de copia externa de los respaldos (USB, OneDrive…),
/// respaldo manual y restauración de un respaldo.
/// </summary>
public class FormConfiguracion : Form
{
    private readonly ConfiguracionService _configuracionService = new();

    private readonly TextBox _txtNombre = Construir.Texto(60, 320);
    private readonly TextBox _txtDireccion = Construir.Texto(120, 320);
    private readonly TextBox _txtTelefono = Construir.Texto(40, 200);
    private readonly TextBox _txtDocumento = Construir.Texto(40, 200, "NIT / NRC");
    private readonly TextBox _txtMensaje = Construir.Texto(120, 320);
    private readonly ComboBox _cboAncho = Construir.Lista(320);
    private readonly Label _lblMensajeNegocio = Construir.Mensaje();

    private readonly TextBox _txtCarpetaExterna = Construir.Texto(260, 360, "Sin copia externa");
    private readonly Label _lblMensajeRespaldo = Construir.Mensaje();

    private sealed record OpcionAncho(int Ancho, string Texto)
    {
        public override string ToString() => Texto;
    }

    public FormConfiguracion()
    {
        Text = "Configuración";
        BackColor = Tema.BlancoLacteo;
        Font = Tema.FuenteBase;
        ClientSize = new Size(820, 440);

        _cboAncho.DataSource = new List<OpcionAncho>
        {
            new(32, "32 caracteres – impresora térmica de 58 mm"),
            new(42, "42 caracteres – impresora térmica de 80 mm"),
            new(48, "48 caracteres – impresora de 80 mm (letra pequeña)")
        };
        _txtCarpetaExterna.ReadOnly = true;

        var negocio = Seccion("Datos del negocio (se imprimen en el ticket)",
            Construir.Campos(
                ("Nombre del negocio", _txtNombre), ("Dirección", _txtDireccion), ("Teléfono", _txtTelefono),
                ("NIT / NRC", _txtDocumento), ("Mensaje al pie", _txtMensaje), ("Ancho del ticket", _cboAncho)),
            Construir.Fila(
                Construir.Boton("Guardar datos", Tema.RolBoton.Primario, (_, _) => GuardarNegocio(), 140),
                Construir.Boton("Ver ticket de prueba", Tema.RolBoton.Secundario, (_, _) => VerTicketPrueba(), 170)),
            _lblMensajeNegocio);

        var respaldos = Seccion("Respaldos",
            Construir.Nota($"Los datos del negocio están en: {DatabaseConnection.DatabaseFolder}\n" +
                           "Cada cierre de caja genera un respaldo automático allí. Para protegerse de una falla del disco, " +
                           "elija además una carpeta en una USB o en OneDrive: cada respaldo automático se copiará también ahí."),
            Construir.Fila(Construir.Etiqueta("Copia externa"), _txtCarpetaExterna),
            Construir.Fila(
                Construir.Boton("Elegir carpeta…", Tema.RolBoton.Secundario, (_, _) => ElegirCarpeta(), 140),
                Construir.Boton("Quitar copia externa", Tema.RolBoton.Secundario, (_, _) => GuardarCarpeta(string.Empty), 170),
                Construir.Boton("Abrir carpeta de datos", Tema.RolBoton.Secundario, (_, _) => AbrirCarpetaDatos(), 180)),
            Construir.Fila(
                Construir.Boton("Hacer respaldo ahora…", Tema.RolBoton.Primario, (_, _) => RespaldarAhora(), 190),
                Construir.Boton("Restaurar un respaldo…", Tema.RolBoton.Peligro, (_, _) => Restaurar(), 200)),
            _lblMensajeRespaldo);

        var raiz = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16, 12, 16, 12)
        };
        raiz.Controls.AddRange(new Control[] { negocio, respaldos });
        Controls.Add(raiz);

        Load += (_, _) => CargarValores();
    }

    private static GroupBox Seccion(string titulo, params Control[] contenido)
    {
        var grupo = new GroupBox
        {
            Text = titulo,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Tema.Crema,
            ForeColor = Tema.CafeRustico,
            MinimumSize = new Size(760, 0)
        };
        var columna = Construir.Columna(contenido);
        columna.Dock = DockStyle.Fill;
        grupo.Controls.Add(columna);
        return grupo;
    }

    private void CargarValores()
    {
        _txtNombre.Text = ConfiguracionNegocio.NombreNegocio;
        _txtDireccion.Text = ConfiguracionNegocio.Direccion;
        _txtTelefono.Text = ConfiguracionNegocio.Telefono;
        _txtDocumento.Text = ConfiguracionNegocio.Documento;
        _txtMensaje.Text = ConfiguracionNegocio.MensajeTicket;
        _cboAncho.SelectedItem = ((List<OpcionAncho>)_cboAncho.DataSource!).First(o => o.Ancho == ConfiguracionNegocio.AnchoTicket);
        _txtCarpetaExterna.Text = ConfiguracionNegocio.CarpetaRespaldoExterna;
    }

    private void GuardarNegocio()
    {
        var ancho = (_cboAncho.SelectedItem as OpcionAncho)?.Ancho ?? ConfiguracionNegocio.AnchoTicketPredeterminado;
        var resultado = _configuracionService.GuardarDatosNegocio(
            _txtNombre.Text, _txtDireccion.Text, _txtTelefono.Text, _txtDocumento.Text, _txtMensaje.Text, ancho);
        Construir.MostrarMensaje(_lblMensajeNegocio, resultado.Mensaje, esError: !resultado.Exitoso);
    }

    private void VerTicketPrueba()
    {
        var venta = new Models.Venta
        {
            IdVenta = 0,
            NombreUsuario = Sesion.Actual?.Nombre,
            Total = 7.75m,
            MetodoPago = Models.Venta.PagoEfectivo,
            MontoRecibido = 10m,
            Cambio = 2.25m,
            Detalles =
            {
                new Models.DetalleVenta { NombreProducto = "Queso fresco", Cantidad = 1.5m, Unidad = Models.UnidadMedida.Libra, PrecioUnitario = 3.50m },
                new Models.DetalleVenta { NombreProducto = "Crema", Cantidad = 1, PrecioUnitario = 2.50m }
            }
        };

        MessageBox.Show(this, TicketHelper.GenerarTexto(venta), "Ticket de prueba (con los datos guardados)",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ElegirCarpeta()
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Carpeta donde se copiará cada respaldo automático (USB, OneDrive…)",
            UseDescriptionForTitle = true
        };

        if (dialogo.ShowDialog(this) == DialogResult.OK)
        {
            GuardarCarpeta(dialogo.SelectedPath);
        }
    }

    private void GuardarCarpeta(string carpeta)
    {
        var resultado = _configuracionService.GuardarCarpetaRespaldoExterna(carpeta);
        _txtCarpetaExterna.Text = ConfiguracionNegocio.CarpetaRespaldoExterna;
        Construir.MostrarMensaje(_lblMensajeRespaldo, resultado.Mensaje, esError: !resultado.Exitoso);
    }

    private void AbrirCarpetaDatos()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = DatabaseConnection.DatabaseFolder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Construir.MostrarMensaje(_lblMensajeRespaldo, $"No se pudo abrir la carpeta: {ex.Message}");
        }
    }

    private void RespaldarAhora()
    {
        using var dialogo = new SaveFileDialog
        {
            Title = "Guardar respaldo de base de datos",
            Filter = "Base de datos SQLite (*.db)|*.db",
            FileName = BackupHelper.GenerarNombreArchivo()
        };

        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        var (exitoso, detalle) = BackupHelper.RealizarBackup(dialogo.FileName);
        Construir.MostrarMensaje(_lblMensajeRespaldo, detalle, esError: !exitoso);
    }

    private void Restaurar()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Elegir el respaldo a restaurar",
            Filter = "Base de datos SQLite (*.db)|*.db",
            InitialDirectory = BackupHelper.CarpetaRespaldosAutomaticos
        };

        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        var confirmar = MessageBox.Show(this,
            "Se reemplazarán TODOS los datos actuales (ventas, inventario, caja…) por los del respaldo:\n\n" +
            $"{dialogo.FileName}\n\nAntes se guardará un respaldo de los datos actuales. El programa se reiniciará.\n\n¿Continuar?",
            "Restaurar respaldo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if (confirmar != DialogResult.Yes) return;

        Cursor = Cursors.WaitCursor;
        var (exitoso, detalle) = BackupHelper.RestaurarDesde(dialogo.FileName);
        Cursor = Cursors.Default;

        if (!exitoso)
        {
            Construir.MostrarMensaje(_lblMensajeRespaldo, detalle);
            return;
        }

        MessageBox.Show(this, detalle, "Restaurar respaldo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Program.ReiniciarAplicacion();
    }
}
