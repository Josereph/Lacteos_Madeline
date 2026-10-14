using LacteosMadeline.Forms;
using LacteosMadeline.Forms.Shell;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Pruebas de humo de la interfaz: abren la ventana principal y recorren
/// los módulos y sus secciones con datos de ejemplo, como administrador y
/// como cajero, y abren cada diálogo. Detectan errores al cargar pantallas
/// (columnas renombradas, servicios que fallan, etc.) que las pruebas de
/// servicios no cubren.
/// </summary>
public class PantallasTests : PruebaConBaseDatos
{
    private void PrepararDatos()
    {
        AbrirCaja(20m);
        var idProducto = CrearProducto("Queso fresco", 3.50m, 10, precioCompra: 2.00m);
        CrearProducto("Queso duro", 4.25m, 5.5m, unidad: UnidadMedida.Libra, codigo: "750100");
        CrearProveedor();
        new ClienteService().Guardar(new Cliente { Nombre = "Tienda La Esquina" });
        var venta = Vender(idProducto, 2).VentaRegistrada!;
        Vender(idProducto, 1);
        Assert.True(new VentaService().Anular(venta.IdVenta, "Prueba de pantallas").Exitoso);
        Assert.True(new MermaService().Registrar(idProducto, 1, Merma.MotivoDanio, null).Exitoso);
        Assert.True(new InventarioService().AjustarExistencia(idProducto, 5).Exitoso);
    }

    /// <summary>Ejecuta la acción en un hilo STA (WinForms) y devuelve los errores que ocurran.</summary>
    private static List<Exception> EnHiloDeInterfaz(Action accion)
    {
        var errores = new List<Exception>();
        var hilo = new Thread(() =>
        {
            // Un error dentro de un evento de la interfaz se registra aquí
            // en lugar de abrir el cuadro de diálogo de error.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, threadScope: true);
            Application.ThreadException += (_, e) => errores.Add(e.Exception);

            try
            {
                accion();
            }
            catch (Exception ex)
            {
                errores.Add(ex);
            }
        });

        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        Assert.True(hilo.Join(TimeSpan.FromSeconds(90)), "La interfaz no respondió.");
        return errores;
    }

    private static List<Form1.Modulo> RecorrerModulos()
    {
        var abiertos = new List<Form1.Modulo>();
        using var principal = new Form1 { ConfirmarAlSalir = false };
        principal.Show();
        Application.DoEvents();

        foreach (var modulo in Enum.GetValues<Form1.Modulo>())
        {
            principal.Navegar(modulo);
            Application.DoEvents();
            if (principal.ModuloActual != modulo) continue;

            abiertos.Add(modulo);
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
        return abiertos;
    }

    [Fact]
    public void Administrador_AbreTodasLasPantallasSinErrores()
    {
        PrepararDatos();
        Sesion.Iniciar(CrearUsuario("Admin", Usuario.RolAdministrador));
        List<Form1.Modulo> abiertos = new();

        var errores = EnHiloDeInterfaz(() => abiertos = RecorrerModulos());

        Assert.Empty(errores);
        Assert.Equal(Enum.GetValues<Form1.Modulo>(), abiertos);
    }

    [Fact]
    public void Cajero_SoloAbreLosModulosDeSuRol()
    {
        PrepararDatos();
        Sesion.Iniciar(CrearUsuario("Cajero", Usuario.RolCajero));
        List<Form1.Modulo> abiertos = new();

        var errores = EnHiloDeInterfaz(() => abiertos = RecorrerModulos());

        Assert.Empty(errores);
        Assert.Equal(
            new[] { Form1.Modulo.PuntoDeVenta, Form1.Modulo.VentasRealizadas, Form1.Modulo.Clientes, Form1.Modulo.Caja },
            abiertos);
    }

    [Fact]
    public void Dialogos_SeAbrenSinErrores()
    {
        CrearUsuario("Admin", Usuario.RolAdministrador);
        Sesion.Iniciar(CrearUsuario("Cajero", Usuario.RolCajero));

        var errores = EnHiloDeInterfaz(() =>
        {
            Form[] dialogos =
            {
                new FormInicioSesion(), new FormPrimerAdministrador(), new FormAutorizacion("Anular una venta"),
                new FormCambiarPin(), new FormEntradaTexto("Motivo", "Escriba el motivo"),
                new FormDescuento(10m, 0m), new FormClienteRapido(),
                new FormInputMonto("Cobro", "Monto", 5m, totalAPagar: 5m)
            };

            foreach (var dialogo in dialogos)
            {
                dialogo.Show();
                Application.DoEvents();
                Assert.True(dialogo.Width > 100 && dialogo.Height > 100, $"{dialogo.GetType().Name} quedó sin tamaño");
                dialogo.Close();
                dialogo.Dispose();
            }
        });

        Assert.Empty(errores);
    }
}
