using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Prueba de humo de la interfaz: abre la ventana principal y recorre
/// todos los módulos y sus secciones con datos de ejemplo. Detecta errores
/// al cargar pantallas (columnas renombradas, servicios que fallan, etc.)
/// que las pruebas de servicios no cubren.
/// </summary>
public class PantallasTests : PruebaConBaseDatos
{
    [Fact]
    public void TodasLasPantallas_AbrenSinErrores()
    {
        AbrirCaja(20m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10, precioCompra: 2.00m);
        CrearProveedor();
        Assert.True(Vender(idProducto, 2).Exitoso);
        Assert.True(new MermaService().Registrar(idProducto, 1, Merma.MotivoDanio, null).Exitoso);

        var errores = new List<Exception>();
        var hilo = new Thread(() =>
        {
            // Un error dentro de un evento de la interfaz se registra aquí
            // en lugar de abrir el cuadro de diálogo de error.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, threadScope: true);
            Application.ThreadException += (_, e) => errores.Add(e.Exception);

            try
            {
                using var principal = new Form1 { ConfirmarAlSalir = false };
                principal.Show();
                Application.DoEvents();

                foreach (var modulo in Enum.GetValues<Form1.Modulo>())
                {
                    principal.Navegar(modulo);
                    Application.DoEvents();

                    if (principal.VistaActual is VistaSecciones secciones)
                    {
                        // Mostrar ignora índices fuera de rango: la sección queda sin abrir.
                        for (var i = 0; ; i++)
                        {
                            secciones.Mostrar(i);
                            Application.DoEvents();
                            if (secciones.Seccion(i) is null) break;
                        }
                    }
                }

                principal.Close();
            }
            catch (Exception ex)
            {
                errores.Add(ex);
            }
        });

        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        Assert.True(hilo.Join(TimeSpan.FromSeconds(60)), "La interfaz no respondió.");

        Assert.Empty(errores);
    }
}
