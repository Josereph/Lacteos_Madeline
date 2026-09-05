namespace LacteosMadeline.Forms;

/// <summary>
/// Formulario auxiliar para solicitar un monto decimal al usuario.
/// Reemplaza la dependencia de Microsoft.VisualBasic.Interaction.InputBox
/// con un diálogo nativo de WinForms (sección 26 lineamientos: mantener
/// coherencia visual con el resto del sistema).
/// </summary>
public class FormInputMonto : Form
{
    private Label _lblMensaje = null!;
    private TextBox _txtMonto = null!;
    private Button _btnAceptar = null!;
    private Button _btnCancelar = null!;

    public decimal Monto { get; private set; }

    public FormInputMonto(string titulo, string mensaje, decimal valorInicial = 0)
    {
        Text = titulo;
        ClientSize = new Size(340, 130);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        _lblMensaje = new Label
        {
            Text = mensaje,
            Location = new Point(12, 15),
            Size = new Size(315, 20),
            AutoSize = false
        };

        _txtMonto = new TextBox
        {
            Location = new Point(12, 42),
            Size = new Size(315, 23),
            Text = valorInicial.ToString("F2")
        };
        _txtMonto.SelectAll();

        _btnAceptar = new Button
        {
            Text = "Aceptar",
            Location = new Point(150, 78),
            Size = new Size(85, 30),
            DialogResult = DialogResult.OK
        };
        _btnAceptar.Click += BtnAceptar_Click;

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Location = new Point(242, 78),
            Size = new Size(85, 30),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = _btnAceptar;
        CancelButton = _btnCancelar;

        Controls.AddRange(new Control[] { _lblMensaje, _txtMonto, _btnAceptar, _btnCancelar });
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
        Monto = valor;
    }
}
