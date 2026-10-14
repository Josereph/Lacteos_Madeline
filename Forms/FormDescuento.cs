using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Helpers;

namespace LacteosMadeline.Forms;

/// <summary>
/// Descuento sobre la venta en curso, en dinero o en porcentaje del
/// subtotal. Muestra en vivo el total resultante.
/// </summary>
public class FormDescuento : DialogoBase
{
    private readonly decimal _subtotal;
    private readonly RadioButton _rbMonto = new() { Text = "Monto ($)", AutoSize = true, Checked = true, Margin = new Padding(0, 4, 16, 4) };
    private readonly RadioButton _rbPorcentaje = new() { Text = "Porcentaje (%)", AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
    private readonly TextBox _txtValor = Construir.Texto(10, 160, "0.00");
    private readonly Label _lblResultado = Construir.Etiqueta(string.Empty, negrita: true, tamano: 12F);

    public bool EsPorcentaje => _rbPorcentaje.Checked;

    public decimal Valor { get; private set; }

    public FormDescuento(decimal subtotal, decimal descuentoActual) : base("Descuento")
    {
        _subtotal = subtotal;
        _txtValor.Font = new Font(Tema.Familia, 13F);
        _txtValor.Text = descuentoActual > 0 ? Dinero.ParaEditar(descuentoActual) : string.Empty;

        Contenido.Controls.AddRange(new Control[]
        {
            Construir.Nota($"Subtotal de la venta: {subtotal:C2}. Escriba 0 para quitar el descuento."),
            Construir.Fila(_rbMonto, _rbPorcentaje),
            _txtValor,
            _lblResultado
        });

        var btnAplicar = Construir.Boton("Aplicar", Tema.RolBoton.Primario, (_, _) => Aplicar());
        Botones.Controls.AddRange(new Control[]
        {
            btnAplicar,
            Construir.Boton("Cancelar", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110)
        });
        AcceptButton = btnAplicar;

        _txtValor.TextChanged += (_, _) => Recalcular();
        _rbMonto.CheckedChanged += (_, _) => Recalcular();
        Shown += (_, _) => { _txtValor.Focus(); _txtValor.SelectAll(); };
        Recalcular();
    }

    private decimal? Descuento()
    {
        if (string.IsNullOrWhiteSpace(_txtValor.Text)) return 0m;
        if (!Dinero.TryParse(_txtValor.Text, out var valor) || valor < 0) return null;
        return EsPorcentaje ? Dinero.Redondear(_subtotal * valor / 100m) : valor;
    }

    private void Recalcular()
    {
        var descuento = Descuento();
        _lblResultado.Text = descuento is decimal d
            ? $"Descuento {d:C2}  ·  Total {Math.Max(0, _subtotal - d):C2}"
            : "Valor no válido";
    }

    private void Aplicar()
    {
        if (!Dinero.TryParse(string.IsNullOrWhiteSpace(_txtValor.Text) ? "0" : _txtValor.Text, out var valor) || valor < 0)
        {
            MostrarError("Escriba un número válido.");
            return;
        }

        Valor = valor;
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>Alta rápida de un cliente desde el punto de venta.</summary>
public class FormClienteRapido : DialogoBase
{
    private readonly Services.ClienteService _clienteService = new();
    private readonly TextBox _txtNombre = Construir.Texto(100, 280);
    private readonly TextBox _txtDocumento = Construir.Texto(30, 180, "DUI / NIT (opcional)");
    private readonly TextBox _txtTelefono = Construir.Texto(30, 180, "Opcional");

    public int IdCliente { get; private set; }

    public FormClienteRapido() : base("Nuevo cliente")
    {
        Contenido.Controls.Add(Construir.Campos(("Nombre", _txtNombre), ("Documento", _txtDocumento), ("Teléfono", _txtTelefono)));

        var btnGuardar = Construir.Boton("Guardar", Tema.RolBoton.Primario, (_, _) => Guardar());
        Botones.Controls.AddRange(new Control[]
        {
            btnGuardar,
            Construir.Boton("Cancelar", Tema.RolBoton.Secundario, (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 110)
        });
        AcceptButton = btnGuardar;
    }

    private void Guardar()
    {
        var resultado = _clienteService.Guardar(new Models.Cliente
        {
            Nombre = _txtNombre.Text,
            Documento = _txtDocumento.Text,
            Telefono = _txtTelefono.Text
        });

        if (!resultado.Exitoso)
        {
            MostrarError(resultado.Mensaje);
            return;
        }

        IdCliente = resultado.IdCliente;
        DialogResult = DialogResult.OK;
        Close();
    }
}
