namespace LacteosMadeline.Forms;

/// <summary>
/// Formulario auxiliar para solicitar un monto decimal al usuario.
/// Reemplaza la dependencia de Microsoft.VisualBasic.Interaction.InputBox
/// con un diálogo nativo de WinForms (sección 26 lineamientos: mantener
/// coherencia visual con el resto del sistema).
/// Si se indica <c>totalAPagar</c>, el diálogo funciona como cobro en
/// efectivo: muestra el cambio en vivo y no acepta montos menores al total
/// (regla RN-12).
/// </summary>
public class FormInputMonto : Form
{
    private Label _lblMensaje = null!;
    private TextBox _txtMonto = null!;
    private Label _lblCambio = null!;
    private Button _btnAceptar = null!;
    private Button _btnCancelar = null!;

    private readonly decimal? _totalAPagar;

    public decimal Monto { get; private set; }

    public FormInputMonto(string titulo, string mensaje, decimal valorInicial = 0, decimal? totalAPagar = null)
    {
        _totalAPagar = totalAPagar;

        Text = titulo;
        ClientSize = new Size(340, totalAPagar is null ? 140 : 178);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        _lblMensaje = new Label
        {
            Text = mensaje,
            Location = new Point(12, 8),
            Size = new Size(315, 40),
            AutoSize = false
        };

        _txtMonto = new TextBox
        {
            Location = new Point(12, 52),
            Size = new Size(315, 23),
            Text = valorInicial.ToString("F2")
        };

        _lblCambio = new Label
        {
            Location = new Point(12, 90),
            Size = new Size(315, 28),
            AutoSize = false,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Visible = totalAPagar is not null
        };

        var filaBotones = totalAPagar is null ? 94 : 130;

        _btnAceptar = new Button
        {
            Text = "Aceptar",
            Location = new Point(150, filaBotones),
            Size = new Size(85, 34),
            DialogResult = DialogResult.OK
        };
        _btnAceptar.Click += BtnAceptar_Click;

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Location = new Point(242, filaBotones),
            Size = new Size(85, 34),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = _btnAceptar;
        CancelButton = _btnCancelar;

        Controls.AddRange(new Control[] { _lblMensaje, _txtMonto, _lblCambio, _btnAceptar, _btnCancelar });

        Tema.Aplicar(this);
        Tema.EstilizarBoton(_btnAceptar, Tema.RolBoton.Primario);
        Tema.EstilizarBoton(_btnCancelar, Tema.RolBoton.Peligro);
        _txtMonto.Font = new Font(Tema.Familia, 12F);
        _lblCambio.Font = new Font(Tema.Familia, 12F, FontStyle.Bold);

        _txtMonto.TextChanged += (_, _) => ActualizarCambio();
        Shown += (_, _) => { _txtMonto.Focus(); _txtMonto.SelectAll(); };
        ActualizarCambio();
    }

    private void ActualizarCambio()
    {
        if (_totalAPagar is not decimal total) return;

        if (!decimal.TryParse(_txtMonto.Text.Trim(), out var recibido))
        {
            _lblCambio.ForeColor = Tema.Alerta;
            _lblCambio.Text = "Monto no válido";
        }
        else if (recibido < total)
        {
            _lblCambio.ForeColor = Tema.Alerta;
            _lblCambio.Text = $"Faltan: {total - recibido:C2}";
        }
        else
        {
            _lblCambio.ForeColor = Tema.Exito;
            _lblCambio.Text = $"Cambio: {recibido - total:C2}";
        }
    }

    private void BtnAceptar_Click(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(_txtMonto.Text.Trim(), out var valor) || valor < 0)
        {
            MessageBox.Show("Ingrese un monto válido (número mayor o igual a cero).",
                "Valor inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None; // Evitar que se cierre
            return;
        }

        if (_totalAPagar is decimal total && valor < total)
        {
            MessageBox.Show($"El monto recibido no cubre el total de la venta ({total:C2}).",
                "Monto insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Monto = valor;
    }
}
