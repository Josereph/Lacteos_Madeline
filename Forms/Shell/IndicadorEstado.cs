using System.Drawing.Drawing2D;

namespace LacteosMadeline.Forms.Shell;

/// <summary>
/// Indicador sobrio de la barra superior: punto de estado o icono
/// vectorial, texto y fondo redondeado. Puede ser clicable para llevar al
/// módulo relacionado (por ejemplo, Caja o el reporte de stock crítico).
/// </summary>
public class IndicadorEstado : Control
{
    private bool _sobre;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Icono Icono { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool MostrarPunto { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color ColorFondo { get; set; } = Tema.Crema;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color ColorTexto { get; set; } = Tema.Carbon;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color ColorAcento { get; set; } = Tema.CafeRustico;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Clicable { get; set; }

    public IndicadorEstado()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
        Font = new Font(Tema.Familia, 9.75F, FontStyle.Bold);
        Height = 36;
    }

    public void Actualizar(string texto, Color fondo, Color colorTexto, Color acento)
    {
        Text = texto;
        ColorFondo = fondo;
        ColorTexto = colorTexto;
        ColorAcento = acento;
        Width = AnchoPreferido();
        Invalidate();
    }

    public int AnchoPreferido()
    {
        var anchoTexto = TextRenderer.MeasureText(Text, Font).Width;
        var anchoMarca = MostrarPunto || Icono != Icono.Ninguno ? 26 : 0;
        return 14 + anchoMarca + anchoTexto + 14;
    }

    protected override void OnMouseEnter(EventArgs e) { _sobre = Clicable; Cursor = Clicable ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _sobre = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Color.White);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var camino = Redondeado(r, 8))
        using (var fondo = new SolidBrush(ColorFondo))
        using (var borde = new Pen(_sobre ? ColorAcento : Color.FromArgb(40, ColorAcento)))
        {
            g.FillPath(fondo, camino);
            g.DrawPath(borde, camino);
        }

        var x = 14;
        if (MostrarPunto)
        {
            using var punto = new SolidBrush(ColorAcento);
            g.FillEllipse(punto, x, (Height - 10) / 2, 10, 10);
            x += 20;
        }
        else if (Icono != Icono.Ninguno)
        {
            IconosVectoriales.Dibujar(g, Icono, new Rectangle(x - 2, (Height - 20) / 2, 20, 20), ColorAcento);
            x += 26;
        }

        TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x - 8, Height), ColorTexto,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private static GraphicsPath Redondeado(Rectangle r, int radio)
    {
        var d = radio * 2;
        var camino = new GraphicsPath();
        camino.AddArc(r.X, r.Y, d, d, 180, 90);
        camino.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        camino.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        camino.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        camino.CloseFigure();
        return camino;
    }
}
