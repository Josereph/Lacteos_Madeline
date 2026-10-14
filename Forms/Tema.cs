namespace LacteosMadeline.Forms;

/// <summary>
/// Identidad visual "Holstein y Café Artesanal" de Lácteos Madeline.
/// Centraliza la paleta y aplica el estilo a cualquier formulario de forma
/// recursiva, sin cambiar nombres de controles ni eventos: cada formulario
/// solo llama <c>Tema.Aplicar(this)</c> después de InitializeComponent().
///
/// Roles de botón (según el nombre del control):
///   Primario  → Confirmar, Guardar, Registrar, Agregar, Abrir, Generar, Ventas
///   Peligro   → Cancelar, Eliminar, CerrarCaja, Anular
///   Secundario→ el resto (Nuevo, Buscar, Imprimir, Salir, …)
/// </summary>
public static class Tema
{
    // ── Paleta corporativa ──────────────────────────────────────────────
    public static readonly Color BlancoLacteo   = ColorTranslator.FromHtml("#F7F6F3"); // fondos
    public static readonly Color Carbon         = ColorTranslator.FromHtml("#221F1E"); // texto / cabeceras
    public static readonly Color CafeRustico    = ColorTranslator.FromHtml("#7B4F31"); // acción principal
    public static readonly Color CafeProfundo   = ColorTranslator.FromHtml("#5C3A21"); // hover acción principal
    public static readonly Color Crema          = ColorTranslator.FromHtml("#EFECE3"); // paneles / zebra
    public static readonly Color Terracota      = ColorTranslator.FromHtml("#A94438"); // alertas / mermas
    public static readonly Color TerracotaOscuro = ColorTranslator.FromHtml("#8A352B"); // hover alertas

    // ── Tonos de apoyo derivados de la paleta ───────────────────────────
    public static readonly Color Exito          = ColorTranslator.FromHtml("#3D6B35"); // confirmaciones
    public static readonly Color GrisCalido     = ColorTranslator.FromHtml("#6E6862"); // texto secundario
    public static readonly Color BordeSuave     = ColorTranslator.FromHtml("#D9D3C7"); // líneas de tabla / bordes
    public static readonly Color CremaHover     = ColorTranslator.FromHtml("#E4DFD2"); // hover secundario
    public static readonly Color TerracotaTenue = ColorTranslator.FromHtml("#F6E3E0"); // campo obligatorio
    public static readonly Color CafeClaro      = ColorTranslator.FromHtml("#C49A74"); // marca sobre fondo oscuro

    // Alias semánticos usados por los formularios para mensajes.
    public static Color Alerta => Terracota;

    // ── Tipografía ──────────────────────────────────────────────────────
    public const string Familia = "Segoe UI";
    public static readonly Font FuenteBase    = new(Familia, 9.75F);
    public static readonly Font FuenteTabla   = new(Familia, 10F);
    public static readonly Font FuenteCabecera = new(Familia, 9.75F, FontStyle.Bold);
    public static readonly Font FuenteTitulo  = new(Familia, 16F, FontStyle.Bold);
    public static readonly Font FuenteBoton   = new(Familia, 9.75F, FontStyle.Bold);

    private static readonly string[] NombresPrimarios =
        { "Confirmar", "Guardar", "Registrar", "Agregar", "Abrir", "Generar", "btnVentas" };

    private static readonly string[] NombresPeligro =
        { "Cancelar", "Eliminar", "CerrarCaja", "Anular" };

    public static void Aplicar(Form form)
    {
        AsignarNombres(form);

        form.SuspendLayout();
        form.BackColor = BlancoLacteo;
        form.ForeColor = Carbon;
        form.Font = FuenteBase;

        foreach (Control control in form.Controls)
        {
            AplicarControl(control);
        }

        form.ResumeLayout(true);
    }

    /// <summary>
    /// Los diseñadores de este proyecto están escritos a mano y no asignan
    /// la propiedad Name. Se toma del nombre del campo (btnConfirmar →
    /// Name = "btnConfirmar") para poder reconocer el rol de cada control.
    /// </summary>
    private static void AsignarNombres(Form form)
    {
        const System.Reflection.BindingFlags campos =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

        foreach (var campo in form.GetType().GetFields(campos))
        {
            if (typeof(Control).IsAssignableFrom(campo.FieldType)
                && campo.GetValue(form) is Control control
                && string.IsNullOrEmpty(control.Name))
            {
                control.Name = campo.Name.TrimStart('_');
            }
        }
    }

    private static void AplicarControl(Control control)
    {
        switch (control)
        {
            case Button boton:
                EstilizarBoton(boton);
                break;

            case DataGridView grid:
                EstilizarTabla(grid);
                break;

            case GroupBox grupo:
                grupo.BackColor = Crema;
                grupo.ForeColor = CafeRustico;
                break;

            case TabPage pagina:
                pagina.BackColor = BlancoLacteo;
                pagina.ForeColor = Carbon;
                break;

            case Panel panel when panel.Name.Contains("Apertura"):
                panel.BackColor = Crema;
                break;

            case TextBox texto:
                texto.BackColor = Color.White;
                texto.ForeColor = Carbon;
                texto.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ComboBox or NumericUpDown or DateTimePicker:
                control.BackColor = Color.White;
                control.ForeColor = Carbon;
                break;

            case Label etiqueta:
                EstilizarEtiqueta(etiqueta);
                break;

            case RadioButton { Appearance: Appearance.Button } opcion:
                EstilizarOpcion(opcion);
                break;

            case RadioButton or CheckBox:
                control.ForeColor = Carbon;
                break;
        }

        foreach (Control hijo in control.Controls)
        {
            AplicarControl(hijo);
        }
    }

    private static void EstilizarEtiqueta(Label etiqueta)
    {
        if (etiqueta.Name == "lblTitulo")
        {
            etiqueta.Font = FuenteTitulo;
            etiqueta.ForeColor = Carbon;
            return;
        }

        // Respeta la intención de los colores puestos en el diseño original,
        // pero llevándolos a la paleta corporativa.
        var c = etiqueta.ForeColor;
        etiqueta.ForeColor =
            c == Color.DarkRed || c == Color.Firebrick || c == Color.Red ? Terracota :
            c == Color.DarkGreen || c == Color.SeaGreen || c == Color.Green ? Exito :
            c == Color.Gray || c == Color.DimGray || c == SystemColors.GrayText ? GrisCalido :
            c == Color.DarkSlateBlue ? CafeRustico :
            Carbon;
    }

    // ── Botones ─────────────────────────────────────────────────────────

    public enum RolBoton { Primario, Secundario, Peligro }

    public static RolBoton DetectarRol(Button boton)
    {
        if (NombresPeligro.Any(n => boton.Name.Contains(n))) return RolBoton.Peligro;
        if (NombresPrimarios.Any(n => boton.Name.Contains(n))) return RolBoton.Primario;
        return RolBoton.Secundario;
    }

    public static void EstilizarBoton(Button boton) => EstilizarBoton(boton, DetectarRol(boton));

    public static void EstilizarBoton(Button boton, RolBoton rol)
    {
        boton.FlatStyle = FlatStyle.Flat;
        boton.Cursor = Cursors.Hand;
        boton.UseVisualStyleBackColor = false;
        boton.FlatAppearance.BorderSize = rol == RolBoton.Secundario ? 1 : 0;

        // Conserva un tamaño de letra mayor si el diseño lo pedía (p. ej. "Nueva Venta").
        var tamano = Math.Max(boton.Font.Size, FuenteBoton.Size);

        switch (rol)
        {
            case RolBoton.Primario:
                boton.BackColor = CafeRustico;
                boton.ForeColor = Color.White;
                boton.FlatAppearance.MouseOverBackColor = CafeProfundo;
                boton.FlatAppearance.MouseDownBackColor = CafeProfundo;
                boton.Font = new Font(Familia, tamano, FontStyle.Bold);
                break;

            case RolBoton.Peligro:
                boton.BackColor = Terracota;
                boton.ForeColor = Color.White;
                boton.FlatAppearance.MouseOverBackColor = TerracotaOscuro;
                boton.FlatAppearance.MouseDownBackColor = TerracotaOscuro;
                boton.Font = new Font(Familia, tamano, FontStyle.Bold);
                break;

            default:
                boton.BackColor = Crema;
                boton.ForeColor = Carbon;
                boton.FlatAppearance.BorderColor = BordeSuave;
                boton.FlatAppearance.MouseOverBackColor = CremaHover;
                boton.FlatAppearance.MouseDownBackColor = BordeSuave;
                boton.Font = new Font(Familia, Math.Max(boton.Font.Size, FuenteBase.Size), boton.Font.Style);
                break;
        }
    }

    /// <summary>
    /// Opción tipo botón (p. ej. método de pago): área amplia, borde suave
    /// y la opción elegida en Café Rústico con texto blanco.
    /// </summary>
    public static void EstilizarOpcion(RadioButton opcion)
    {
        opcion.FlatStyle = FlatStyle.Flat;
        opcion.TextAlign = ContentAlignment.MiddleCenter;
        opcion.Cursor = Cursors.Hand;
        opcion.UseVisualStyleBackColor = false;
        opcion.BackColor = Color.White;
        opcion.FlatAppearance.BorderColor = BordeSuave;
        opcion.FlatAppearance.BorderSize = 1;
        opcion.FlatAppearance.CheckedBackColor = CafeRustico;
        opcion.FlatAppearance.MouseOverBackColor = CremaHover;
        opcion.Font = new Font(Familia, Math.Max(opcion.Font.Size, FuenteBoton.Size), FontStyle.Bold);

        void Colorear() => opcion.ForeColor = opcion.Checked ? Color.White : Carbon;
        opcion.CheckedChanged += (_, _) => Colorear();
        Colorear();
    }

    // ── Tablas ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tabla limpia: sin bordes pesados, cabecera Negro Holstein con texto
    /// blanco, filas alternadas blanco / crema y selección en café.
    /// Al enlazar datos ajusta alineación y formato según el tipo de cada
    /// columna (montos en C2 a la derecha, números a la derecha, texto a la
    /// izquierda, fechas dd/MM/yyyy HH:mm).
    /// </summary>
    public static void EstilizarTabla(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.GridColor = BordeSuave;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Carbon;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Carbon;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = FuenteCabecera;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Carbon;
        grid.DefaultCellStyle.Font = FuenteTabla;
        grid.DefaultCellStyle.SelectionBackColor = CafeRustico;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Crema;
        grid.RowTemplate.Height = 34;

        grid.DataBindingComplete -= AjustarColumnas;
        grid.DataBindingComplete += AjustarColumnas;
        grid.HandleCreated -= AlCrearTabla;
        grid.HandleCreated += AlCrearTabla;
    }

    private const string AnchosPendientes = "Tema:AnchosPendientes";

    private static void AlCrearTabla(object? sender, EventArgs e)
    {
        if (sender is DataGridView grid) ProgramarAnchoMinimo(grid);
    }

    /// <summary>
    /// Los formularios renombran las cabeceras justo después de enlazar
    /// datos, así que el ancho mínimo se calcula un instante después
    /// (BeginInvoke: corre antes del primer pintado), con el texto
    /// definitivo. Se hace una sola vez por enlace, sin repintados extra.
    /// </summary>
    private static void ProgramarAnchoMinimo(DataGridView grid)
    {
        if (!Equals(grid.Tag, AnchosPendientes) || !grid.IsHandleCreated) return;
        grid.BeginInvoke(() => AjustarAnchoMinimoCabeceras(grid));
    }

    private static void AjustarAnchoMinimoCabeceras(DataGridView grid)
    {
        if (grid.IsDisposed || !Equals(grid.Tag, AnchosPendientes)) return;
        grid.Tag = null;

        // Cambiar anchos con el modo Fill activo recalcula todas las columnas
        // en cada asignación; se pausa y se recalcula una sola vez al final.
        var modo = grid.AutoSizeColumnsMode;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        try
        {
            AplicarAnchoMinimo(grid);
        }
        finally
        {
            grid.AutoSizeColumnsMode = modo;
        }
    }

    private static void AplicarAnchoMinimo(DataGridView grid)
    {

        var visibles = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
        var anchos = visibles.ToDictionary(c => c, c => TextRenderer.MeasureText(c.HeaderText, FuenteCabecera).Width + 16);

        // Si todas las cabeceras caben en una línea se fija su ancho mínimo;
        // si no (tablas angostas con muchas columnas), se parten en dos
        // líneas para no forzar una barra de desplazamiento horizontal.
        // Se deja ~45% del ancho libre para que las columnas de texto respiren.
        if (anchos.Values.Sum() <= grid.ClientSize.Width * 0.55)
        {
            foreach (var (columna, ancho) in anchos)
            {
                if (columna.MinimumWidth < ancho) columna.MinimumWidth = ancho;
            }
        }
        else
        {
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersHeight = 50;

            // Una palabra sola no se puede partir: su ancho es el mínimo.
            foreach (var columna in visibles)
            {
                var palabraMasLarga = columna.HeaderText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => TextRenderer.MeasureText(p, FuenteCabecera).Width + 16)
                    .DefaultIfEmpty(0).Max();
                if (columna.MinimumWidth < palabraMasLarga) columna.MinimumWidth = palabraMasLarga;
            }
        }
    }

    private static void AjustarColumnas(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        if (sender is not DataGridView grid) return;

        // Solo marca la tabla si nadie más usa Tag.
        if (grid.Tag is null) grid.Tag = AnchosPendientes;

        var modoRelleno = grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill;
        if (modoRelleno) grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        try
        {
            FormatearColumnas(grid, modoRelleno);
        }
        finally
        {
            if (modoRelleno) grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        ProgramarAnchoMinimo(grid);
    }

    private static void FormatearColumnas(DataGridView grid, bool modoRelleno)
    {
        foreach (DataGridViewColumn columna in grid.Columns)
        {
            // Las listas enlazadas no se ordenan; sin la flecha de orden
            // la cabecera aprovecha todo el ancho.
            columna.SortMode = DataGridViewColumnSortMode.NotSortable;

            var tipo = Nullable.GetUnderlyingType(columna.ValueType ?? typeof(object)) ?? columna.ValueType;
            var estilo = columna.DefaultCellStyle;
            var cabecera = columna.HeaderCell.Style;

            // En tablas que llenan el ancho, el espacio se reparte según el
            // contenido: nombres anchos, montos medianos, banderas angostas.
            if (modoRelleno)
            {
                columna.MinimumWidth = 40;
                columna.FillWeight =
                    columna.DataPropertyName is "Nombre" or "NombreProducto" or "Producto" or "Descripcion" ? 220 :
                    tipo == typeof(string) ? 140 :
                    tipo == typeof(DateTime) ? 150 :
                    tipo == typeof(decimal) ? 95 :
                    tipo == typeof(bool) ? 60 :
                    columna.DataPropertyName.StartsWith("Id") ? 55 :
                    80;
            }

            if (tipo == typeof(decimal))
            {
                if (string.IsNullOrEmpty(estilo.Format)) estilo.Format = "C2";
                estilo.Alignment = DataGridViewContentAlignment.MiddleRight;
                cabecera.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            else if (tipo == typeof(int) || tipo == typeof(long) || tipo == typeof(double))
            {
                estilo.Alignment = DataGridViewContentAlignment.MiddleRight;
                cabecera.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            else if (tipo == typeof(DateTime))
            {
                if (string.IsNullOrEmpty(estilo.Format)) estilo.Format = "dd/MM/yyyy HH:mm";
                estilo.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            else if (tipo == typeof(bool))
            {
                estilo.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cabecera.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
                estilo.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }
    }
}
