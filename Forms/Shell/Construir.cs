using LacteosMadeline.Helpers;
using LacteosMadeline.Models;

namespace LacteosMadeline.Forms.Shell;

/// <summary>
/// Piezas para armar pantallas y diálogos por código con la identidad
/// visual del sistema (las pantallas nuevas no usan coordenadas fijas:
/// se acomodan solas con TableLayoutPanel).
/// </summary>
public static class Construir
{
    public static Label Etiqueta(string texto, bool negrita = false, float tamano = 9.75F) => new()
    {
        Text = texto,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 8, 6),
        Font = new Font(Tema.Familia, tamano, negrita ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = Tema.Carbon
    };

    public static Label Titulo(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 4),
        Font = new Font(Tema.Familia, 15F, FontStyle.Bold),
        ForeColor = Tema.Carbon
    };

    public static Label Nota(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        MaximumSize = new Size(560, 0),
        Margin = new Padding(0, 0, 0, 10),
        Font = new Font(Tema.Familia, 9F),
        ForeColor = Tema.GrisCalido
    };

    public static Label Mensaje() => new()
    {
        AutoSize = true,
        MaximumSize = new Size(760, 0),
        Margin = new Padding(0, 8, 0, 0),
        Font = new Font(Tema.Familia, 9.75F, FontStyle.Bold),
        ForeColor = Tema.Alerta
    };

    public static TextBox Texto(int maxLength = 200, int ancho = 260, string? ayuda = null) => new()
    {
        MaxLength = maxLength,
        Width = ancho,
        PlaceholderText = ayuda ?? string.Empty,
        Margin = new Padding(0, 3, 0, 3)
    };

    public static TextBox Pin() => new()
    {
        MaxLength = Helpers.Pin.LongitudMaxima,
        Width = 160,
        UseSystemPasswordChar = true,
        Font = new Font(Tema.Familia, 16F),
        TextAlign = HorizontalAlignment.Center,
        Margin = new Padding(0, 3, 0, 3)
    };

    public static ComboBox Lista(int ancho = 260) => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = ancho,
        Margin = new Padding(0, 3, 0, 3)
    };

    public static Button Boton(string texto, Tema.RolBoton rol, EventHandler alPulsar, int ancho = 130)
    {
        var boton = new Button
        {
            Text = texto,
            AutoSize = false,
            Size = new Size(ancho, 36),
            Margin = new Padding(0, 4, 8, 4)
        };
        boton.Click += alPulsar;
        Tema.EstilizarBoton(boton, rol);
        return boton;
    }

    /// <summary>Rejilla de dos columnas: etiqueta a la izquierda, control a la derecha.</summary>
    public static TableLayoutPanel Campos(params (string Etiqueta, Control Control)[] campos)
    {
        var rejilla = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0, 4, 0, 4)
        };
        rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        foreach (var (etiqueta, control) in campos)
        {
            rejilla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rejilla.Controls.Add(Etiqueta(etiqueta));
            rejilla.Controls.Add(control);
        }

        return rejilla;
    }

    /// <summary>Fila horizontal de controles (botones, filtros).</summary>
    public static FlowLayoutPanel Fila(params Control[] controles)
    {
        var fila = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 4, 0, 4)
        };
        fila.Controls.AddRange(controles);
        return fila;
    }

    /// <summary>Columna vertical que apila secciones de arriba hacia abajo.</summary>
    public static FlowLayoutPanel Columna(params Control[] controles)
    {
        var columna = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };
        columna.Controls.AddRange(controles);
        return columna;
    }

    /// <summary>Tabla de solo lectura con el estilo del sistema.</summary>
    public static DataGridView Tabla()
    {
        var tabla = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        Tema.EstilizarTabla(tabla);
        return tabla;
    }

    public static void MostrarMensaje(Label etiqueta, string mensaje, bool esError = true)
    {
        etiqueta.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        etiqueta.Text = mensaje;
    }

    /// <summary>
    /// Ajusta un selector de cantidad a la unidad del producto: entero para
    /// "Unidad", hasta 3 decimales para peso o volumen. Vuelve a 1 para no
    /// dejar un valor con decimales en un producto que se vende por unidad.
    /// </summary>
    public static void ConfigurarCantidad(NumericUpDown selector, Producto? producto, decimal maximo = 100000)
    {
        var decimales = producto?.PermiteDecimales == true;
        selector.DecimalPlaces = decimales ? Cantidades.Decimales : 0;
        selector.Increment = 1;
        selector.Minimum = decimales ? 0.001m : 1;
        selector.Maximum = Math.Max(selector.Minimum, maximo);
        selector.Value = Math.Min(1, selector.Maximum);
    }
}

/// <summary>
/// Base de los diálogos breves (inicio de sesión, autorización, PIN…):
/// ventana fija centrada, contenido apilado y fila de botones al pie.
/// </summary>
public class DialogoBase : Form
{
    protected readonly FlowLayoutPanel Contenido;
    protected readonly FlowLayoutPanel Botones;
    protected readonly Label LblMensaje = Construir.Mensaje();

    public DialogoBase(string titulo)
    {
        Text = titulo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Tema.BlancoLacteo;
        Font = Tema.FuenteBase;
        Padding = new Padding(20, 16, 20, 14);

        Contenido = Construir.Columna();
        Botones = Construir.Fila();
        Botones.Margin = new Padding(0, 12, 0, 0);

        var raiz = Construir.Columna(Contenido, LblMensaje, Botones);
        Controls.Add(raiz);
    }

    protected void MostrarError(string mensaje) => Construir.MostrarMensaje(LblMensaje, mensaje);
}
