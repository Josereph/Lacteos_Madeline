using System.Drawing.Drawing2D;

namespace LacteosMadeline.Forms.Shell;

/// <summary>
/// Iconografía vectorial monocromática dibujada con GDI+ (líneas y
/// formas geométricas simples, estilo flat corporativo). No depende de
/// fuentes de símbolos ni de emojis, por lo que se ve igual en cualquier
/// equipo Windows y escala sin perder nitidez.
/// Cada icono se dibuja en una caja cuadrada; el trazo se ajusta al tamaño.
/// </summary>
public enum Icono
{
    Ninguno,
    PuntoDeVenta,
    Productos,
    Compras,
    Caja,
    Mermas,
    Reportes,
    Proveedores,
    Respaldo,
    Salir,
    Reloj,
    Alerta,
    Cliente,
    Recibo,
    Engrane,
    Candado,
    Usuario
}

public static class IconosVectoriales
{
    public static void Dibujar(Graphics g, Icono icono, Rectangle caja, Color color)
    {
        if (icono == Icono.Ninguno) return;

        var estadoPrevio = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Se trabaja en una cuadrícula lógica de 24×24 escalada a la caja.
        var escala = Math.Min(caja.Width, caja.Height) / 24f;
        g.TranslateTransform(caja.X + (caja.Width - 24 * escala) / 2, caja.Y + (caja.Height - 24 * escala) / 2);
        g.ScaleTransform(escala, escala);

        using var trazo = new Pen(color, 1.8f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var relleno = new SolidBrush(color);

        switch (icono)
        {
            case Icono.PuntoDeVenta: // carrito
                g.DrawLines(trazo, new PointF[] { new(2, 4), new(5, 4), new(7.5f, 15), new(19, 15), new(21, 7), new(6, 7) });
                g.FillEllipse(relleno, 7.5f, 17.5f, 3, 3);
                g.FillEllipse(relleno, 16, 17.5f, 3, 3);
                break;

            case Icono.Productos: // caja de producto
                g.DrawRectangle(trazo, 3.5f, 8, 17, 12.5f);
                g.DrawLines(trazo, new PointF[] { new(3.5f, 8), new(6, 3.5f), new(18, 3.5f), new(20.5f, 8) });
                g.DrawLine(trazo, 9.5f, 12, 14.5f, 12);
                break;

            case Icono.Compras: // camión de reparto
                g.DrawRectangle(trazo, 2, 6, 12, 10);
                g.DrawLines(trazo, new PointF[] { new(14, 9), new(18.5f, 9), new(22, 12.5f), new(22, 16), new(14, 16) });
                g.FillEllipse(relleno, 4.5f, 16, 4, 4);
                g.FillEllipse(relleno, 16, 16, 4, 4);
                break;

            case Icono.Caja: // caja registradora
                g.DrawRectangle(trazo, 3, 11, 18, 9);
                g.DrawRectangle(trazo, 6, 4, 9, 4.5f);
                g.DrawLine(trazo, 10.5f, 8.5f, 10.5f, 11);
                g.DrawLine(trazo, 3, 15.5f, 21, 15.5f);
                g.DrawLine(trazo, 10, 18, 14, 18);
                break;

            case Icono.Mermas: // contenedor de desecho
                g.DrawLine(trazo, 3.5f, 6, 20.5f, 6);
                g.DrawLines(trazo, new PointF[] { new(9, 6), new(9.5f, 3.5f), new(14.5f, 3.5f), new(15, 6) });
                g.DrawLines(trazo, new PointF[] { new(5.5f, 6), new(7, 21), new(17, 21), new(18.5f, 6) });
                g.DrawLine(trazo, 10, 10, 10.3f, 17);
                g.DrawLine(trazo, 14, 10, 13.7f, 17);
                break;

            case Icono.Reportes: // gráfico de barras
                g.DrawLine(trazo, 3, 21, 21, 21);
                g.FillRectangle(relleno, 5, 13, 3.5f, 6);
                g.FillRectangle(relleno, 10.5f, 8, 3.5f, 11);
                g.FillRectangle(relleno, 16, 4, 3.5f, 15);
                break;

            case Icono.Proveedores: // dos personas
                g.DrawEllipse(trazo, 5, 4, 6, 6);
                g.DrawArc(trazo, 2, 12, 12, 14, 180, 180);
                g.DrawEllipse(trazo, 14, 5.5f, 5, 5);
                g.DrawArc(trazo, 13, 13, 9, 11, 200, 160);
                break;

            case Icono.Respaldo: // base de datos
                g.DrawEllipse(trazo, 4, 3, 16, 5);
                g.DrawLine(trazo, 4, 5.5f, 4, 18.5f);
                g.DrawLine(trazo, 20, 5.5f, 20, 18.5f);
                g.DrawArc(trazo, 4, 9.5f, 16, 5, 0, 180);
                g.DrawArc(trazo, 4, 16, 16, 5, 0, 180);
                break;

            case Icono.Salir: // puerta con flecha de salida
                g.DrawLines(trazo, new PointF[] { new(13, 4), new(4, 4), new(4, 20), new(13, 20) });
                g.DrawLine(trazo, 9, 12, 21, 12);
                g.DrawLines(trazo, new PointF[] { new(17, 8), new(21, 12), new(17, 16) });
                break;

            case Icono.Reloj:
                g.DrawEllipse(trazo, 3, 3, 18, 18);
                g.DrawLines(trazo, new PointF[] { new(12, 7), new(12, 12), new(15.5f, 14) });
                break;

            case Icono.Cliente: // persona con etiqueta
                g.DrawEllipse(trazo, 8, 3, 8, 8);
                g.DrawArc(trazo, 4, 13, 16, 16, 180, 180);
                break;

            case Icono.Usuario: // persona
                g.DrawEllipse(trazo, 8, 3.5f, 8, 8);
                g.DrawArc(trazo, 4, 14, 16, 14, 180, 180);
                break;

            case Icono.Recibo: // ticket con borde dentado
                g.DrawLines(trazo, new PointF[] { new(5, 3), new(19, 3), new(19, 21), new(17, 19.5f), new(15, 21), new(13, 19.5f), new(11, 21), new(9, 19.5f), new(7, 21), new(5, 19.5f), new(5, 3) });
                g.DrawLine(trazo, 8.5f, 8, 15.5f, 8);
                g.DrawLine(trazo, 8.5f, 11.5f, 15.5f, 11.5f);
                g.DrawLine(trazo, 8.5f, 15, 12.5f, 15);
                break;

            case Icono.Engrane: // engranaje
                g.DrawEllipse(trazo, 7, 7, 10, 10);
                g.DrawEllipse(trazo, 10.3f, 10.3f, 3.4f, 3.4f);
                for (var i = 0; i < 8; i++)
                {
                    var angulo = i * Math.PI / 4;
                    g.DrawLine(trazo,
                        12 + (float)(Math.Cos(angulo) * 5.5), 12 + (float)(Math.Sin(angulo) * 5.5),
                        12 + (float)(Math.Cos(angulo) * 8.5), 12 + (float)(Math.Sin(angulo) * 8.5));
                }
                break;

            case Icono.Candado: // candado
                g.DrawRectangle(trazo, 5, 11, 14, 10);
                g.DrawArc(trazo, 8, 4, 8, 12, 180, 180);
                g.DrawLine(trazo, 8, 10, 8, 11);
                g.DrawLine(trazo, 16, 10, 16, 11);
                g.FillEllipse(relleno, 10.8f, 14.5f, 2.4f, 2.4f);
                break;

            case Icono.Alerta: // triángulo de advertencia
                g.DrawPolygon(trazo, new PointF[] { new(12, 3), new(22, 20.5f), new(2, 20.5f) });
                g.DrawLine(trazo, 12, 9.5f, 12, 14.5f);
                g.FillEllipse(relleno, 10.9f, 16.4f, 2.2f, 2.2f);
                break;
        }

        g.Restore(estadoPrevio);
    }
}
