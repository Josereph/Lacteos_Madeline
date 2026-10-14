namespace LacteosMadeline.Forms.Shell;

/// <summary>
/// Incrusta formularios dentro de un panel del shell en lugar de abrirlos
/// como ventanas flotantes: sin borde, sin barra de título y ocupando todo
/// el espacio disponible.
/// </summary>
public static class Navegacion
{
    public static void Incrustar(Control contenedor, Form formulario)
    {
        var desplazamiento = OcultarTituloDuplicado(formulario);

        // Tamaño de diseño original: si la pantalla fuera más pequeña,
        // aparecen barras de desplazamiento en lugar de encimar controles.
        var tamanoDiseno = new Size(formulario.ClientSize.Width, formulario.ClientSize.Height - desplazamiento);

        formulario.TopLevel = false;
        formulario.FormBorderStyle = FormBorderStyle.None;
        formulario.Dock = DockStyle.Fill;
        formulario.AutoScroll = true;
        formulario.AutoScrollMinSize = tamanoDiseno;

        contenedor.Controls.Add(formulario);
        formulario.Show();
        formulario.BringToFront();
    }

    /// <summary>
    /// La barra superior del shell ya muestra el nombre del módulo. Si la
    /// pantalla tiene su propio título (lblTitulo) arriba, se oculta y el
    /// resto del contenido sube para aprovechar el espacio.
    /// Devuelve cuántos píxeles subió el contenido.
    /// </summary>
    private static int OcultarTituloDuplicado(Form formulario)
    {
        var titulo = formulario.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblTitulo");
        if (titulo is null) return 0;

        var debajo = formulario.Controls.Cast<Control>()
            .Where(c => c != titulo && c.Top >= titulo.Top)
            .ToList();
        if (debajo.Count == 0) return 0;

        var desplazamiento = debajo.Min(c => c.Top) - titulo.Top;
        titulo.Visible = false;

        formulario.SuspendLayout();
        foreach (var control in debajo)
        {
            control.Top -= desplazamiento;
        }
        formulario.ResumeLayout(false);

        return desplazamiento;
    }
}

/// <summary>
/// Utilidades para que las pantallas diseñadas con posiciones fijas se
/// expandan con la ventana maximizada.
/// </summary>
public static class Diseno
{
    public const AnchorStyles Todo = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    public const AnchorStyles ArribaAncho = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    public const AnchorStyles AbajoAncho = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    public const AnchorStyles AbajoIzquierda = AnchorStyles.Bottom | AnchorStyles.Left;
    public const AnchorStyles AbajoDerecha = AnchorStyles.Bottom | AnchorStyles.Right;
    public const AnchorStyles ArribaDerecha = AnchorStyles.Top | AnchorStyles.Right;

    public static void Anclar(AnchorStyles anclaje, params Control[] controles)
    {
        foreach (var control in controles)
        {
            control.Anchor = anclaje;
        }
    }

    // Las columnas de las tablas se generan a partir de las propiedades del
    // modelo enlazado. Estas utilidades las ajustan por nombre y no hacen
    // nada si la columna no existe (por ejemplo, si la tabla está vacía o
    // el modelo cambió), en lugar de fallar con NullReferenceException.

    public static void Encabezado(this DataGridView tabla, string columna, string texto)
    {
        if (tabla.Columns[columna] is { } c) c.HeaderText = texto;
    }

    public static void OcultarColumna(this DataGridView tabla, string columna)
    {
        if (tabla.Columns[columna] is { } c) c.Visible = false;
    }

    public static void FormatoColumna(this DataGridView tabla, string columna, string formato)
    {
        if (tabla.Columns[columna] is { } c) c.DefaultCellStyle.Format = formato;
    }
}

/// <summary>
/// Panel con doble búfer: evita el parpadeo al cambiar de módulo.
/// </summary>
public class PanelSinParpadeo : Panel
{
    public PanelSinParpadeo()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }
}

/// <summary>
/// Módulo con varias pantallas relacionadas (por ejemplo, Productos y
/// Categorías): una barra de secciones arriba y la pantalla elegida debajo.
/// Cada pantalla se crea la primera vez que se abre y conserva su estado
/// mientras el módulo esté abierto.
/// </summary>
public class VistaSecciones : UserControl
{
    private readonly FlowLayoutPanel _barra = new();
    private readonly PanelSinParpadeo _contenido = new();
    private readonly List<(Button Boton, Func<Form> Crear)> _secciones = new();
    private readonly Dictionary<int, Form> _abiertas = new();

    public VistaSecciones(params (string Titulo, Func<Form> Crear)[] secciones)
    {
        BackColor = Tema.BlancoLacteo;
        Dock = DockStyle.Fill;

        _barra.Dock = DockStyle.Top;
        _barra.Height = 58;
        _barra.Padding = new Padding(16, 12, 16, 0);
        _barra.BackColor = Tema.BlancoLacteo;
        _barra.WrapContents = false;

        _contenido.Dock = DockStyle.Fill;
        _contenido.BackColor = Tema.BlancoLacteo;

        Controls.Add(_contenido);
        Controls.Add(_barra);

        for (var i = 0; i < secciones.Length; i++)
        {
            var indice = i;
            var boton = new Button
            {
                Text = secciones[i].Titulo,
                Height = 40,
                Width = TextRenderer.MeasureText(secciones[i].Titulo, Tema.FuenteBoton).Width + 48,
                Margin = new Padding(0, 0, 8, 0),
                Name = "btnSeccion" + i
            };
            boton.Click += (_, _) => Mostrar(indice);
            _barra.Controls.Add(boton);
            _secciones.Add((boton, secciones[i].Crear));
        }

        // Pequeña línea divisoria bajo la barra de secciones.
        _barra.Paint += (_, e) =>
        {
            using var linea = new Pen(Tema.BordeSuave);
            e.Graphics.DrawLine(linea, 16, _barra.Height - 1, _barra.Width - 16, _barra.Height - 1);
        };

        Load += (_, _) => Mostrar(0);
    }

    public void Mostrar(int indice)
    {
        if (indice < 0 || indice >= _secciones.Count) return;

        _contenido.SuspendLayout();
        if (!_abiertas.TryGetValue(indice, out var formulario) || formulario.IsDisposed)
        {
            formulario = _secciones[indice].Crear();
            _abiertas[indice] = formulario;
            Navegacion.Incrustar(_contenido, formulario);
        }

        foreach (var otro in _abiertas.Values.Where(f => f != formulario)) otro.Hide();
        formulario.Show();
        formulario.BringToFront();
        _contenido.ResumeLayout(true);

        for (var i = 0; i < _secciones.Count; i++)
        {
            Tema.EstilizarBoton(_secciones[i].Boton, i == indice ? Tema.RolBoton.Primario : Tema.RolBoton.Secundario);
        }
    }

    public Form? Seccion(int indice) => _abiertas.GetValueOrDefault(indice);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var f in _abiertas.Values) f.Dispose();
        }
        base.Dispose(disposing);
    }
}
