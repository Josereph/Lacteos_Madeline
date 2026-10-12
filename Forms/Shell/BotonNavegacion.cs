namespace LacteosMadeline.Forms.Shell;

/// <summary>
/// Botón de la barra lateral: icono vectorial, texto alineado a la
/// izquierda y área de clic amplia. El módulo activo se resalta con fondo
/// Café Rústico y un indicador vertical Crema en el borde izquierdo.
/// </summary>
public class BotonNavegacion : Button
{
    private bool _activo;
    private bool _sobre;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Icono Icono { get; set; }

    /// <summary>Estilo delimitado para acciones de sistema (Salir, Respaldo).</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool EsAccionSistema { get; set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Activo
    {
        get => _activo;
        set { _activo = value; Invalidate(); }
    }

    public BotonNavegacion()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Height = 52;
        Font = new Font(Tema.Familia, 10.5F);
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e) { _sobre = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _sobre = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var fondoBarra = Parent?.BackColor ?? Tema.Carbon;

        Color fondo, texto;
        if (_activo)
        {
            fondo = Tema.CafeRustico;
            texto = Color.White;
        }
        else if (_sobre)
        {
            fondo = ColorTranslator.FromHtml("#36312F");
            texto = Color.White;
        }
        else
        {
            fondo = fondoBarra;
            texto = Tema.Crema;
        }

        g.Clear(fondoBarra);

        var area = ClientRectangle;
        if (EsAccionSistema)
        {
            // Acción delimitada: recuadro con borde, separada de la navegación.
            area = new Rectangle(16, 4, Width - 32, Height - 8);
            using var borde = new Pen(_sobre ? Tema.Crema : ColorTranslator.FromHtml("#5A5350"));
            using var relleno = new SolidBrush(_sobre ? ColorTranslator.FromHtml("#36312F") : fondoBarra);
            g.FillRectangle(relleno, area);
            g.DrawRectangle(borde, area.X, area.Y, area.Width - 1, area.Height - 1);
        }
        else
        {
            using var relleno = new SolidBrush(fondo);
            g.FillRectangle(relleno, area);

            if (_activo)
            {
                using var indicador = new SolidBrush(Tema.Crema);
                g.FillRectangle(indicador, 0, 0, 5, Height);
            }
        }

        var xIcono = area.X + (EsAccionSistema ? 14 : 24);
        IconosVectoriales.Dibujar(g, Icono, new Rectangle(xIcono, area.Y + (area.Height - 22) / 2, 22, 22), texto);

        var areaTexto = new Rectangle(xIcono + 36, area.Y, area.Right - xIcono - 40, area.Height);
        var fuente = _activo ? new Font(Font, FontStyle.Bold) : Font;
        TextRenderer.DrawText(g, Text, fuente, areaTexto, texto,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (_activo) fuente.Dispose();

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(area, -3, -3), texto, fondo);
        }
    }
}
