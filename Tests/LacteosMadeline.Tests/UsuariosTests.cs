using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

namespace LacteosMadeline.Tests;

/// <summary>
/// Usuarios: PIN cifrado, inicio de sesión, reglas de PIN y que nunca quede
/// el sistema sin un administrador activo.
/// </summary>
public class UsuariosTests : PruebaConBaseDatos
{
    [Fact]
    public void BaseNueva_RequiereCrearElPrimerAdministrador()
    {
        var servicio = new UsuarioService();
        Assert.True(servicio.RequiereConfiguracionInicial());

        var resultado = servicio.CrearPrimerAdministrador("Madeline", "2580", "2580");

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.True(resultado.Usuario!.EsAdministrador);
        Assert.False(servicio.RequiereConfiguracionInicial());
        Assert.False(servicio.CrearPrimerAdministrador("Otro", "2580", "2580").Exitoso);
    }

    [Fact]
    public void ElPinSeGuardaCifradoYSoloElCorrectoPermiteEntrar()
    {
        var usuario = CrearUsuario("Ana", Usuario.RolCajero, pin: "7391");

        Assert.DoesNotContain("7391", Escalar<string>($"SELECT PinHuella || PinSal FROM Usuarios WHERE IdUsuario = {usuario.IdUsuario};"));
        Assert.True(new UsuarioService().Autenticar(usuario.IdUsuario, "7391").Exitoso);
        Assert.False(new UsuarioService().Autenticar(usuario.IdUsuario, "7392").Exitoso);
        Assert.False(new UsuarioService().Autenticar(usuario.IdUsuario, "").Exitoso);
    }

    [Theory]
    [InlineData("123")]       // muy corto
    [InlineData("1234567")]   // muy largo
    [InlineData("12a4")]      // no numérico
    [InlineData("1111")]      // mismo número repetido
    public void PinInvalido_SeRechaza(string pin)
    {
        Assert.False(Pin.Validar(pin).EsValido);
        Assert.False(new UsuarioService().Crear("Ana", Usuario.RolCajero, pin, pin).Exitoso);
    }

    [Fact]
    public void ConfirmacionDistinta_ONombreRepetido_SeRechaza()
    {
        var servicio = new UsuarioService();
        Assert.False(servicio.Crear("Ana", Usuario.RolCajero, "2468", "2469").Exitoso);

        CrearUsuario("Ana", Usuario.RolCajero);
        Assert.False(servicio.Crear("ANA", Usuario.RolCajero, "1357", "1357").Exitoso);
    }

    [Fact]
    public void UsuarioInactivo_NoPuedeEntrar()
    {
        CrearUsuario("Admin", Usuario.RolAdministrador);
        var cajero = CrearUsuario("Cajero", Usuario.RolCajero);

        Assert.True(new UsuarioService().AlternarEstado(cajero.IdUsuario).Exitoso);

        Assert.False(new UsuarioService().Autenticar(cajero.IdUsuario, "2468").Exitoso);
    }

    [Fact]
    public void NoSePuedeDejarElSistemaSinAdministrador()
    {
        var admin = CrearUsuario("Admin", Usuario.RolAdministrador);
        var servicio = new UsuarioService();

        Assert.False(servicio.AlternarEstado(admin.IdUsuario).Exitoso);
        Assert.False(servicio.Modificar(admin.IdUsuario, "Admin", Usuario.RolCajero).Exitoso);

        CrearUsuario("Segunda", Usuario.RolAdministrador);
        Assert.True(servicio.Modificar(admin.IdUsuario, "Admin", Usuario.RolCajero).Exitoso);
    }

    [Fact]
    public void CambiarPropioPin_ExigeElPinActual()
    {
        var usuario = CrearUsuario("Ana", Usuario.RolCajero, pin: "2468");
        var servicio = new UsuarioService();

        Assert.False(servicio.CambiarPropioPin(usuario.IdUsuario, "9999", "1357", "1357").Exitoso);
        Assert.True(servicio.CambiarPropioPin(usuario.IdUsuario, "2468", "1357", "1357").Exitoso);
        Assert.True(servicio.Autenticar(usuario.IdUsuario, "1357").Exitoso);
        Assert.False(servicio.Autenticar(usuario.IdUsuario, "2468").Exitoso);
    }

    [Fact]
    public void SoloUnAdministradorPuedeAutorizar()
    {
        var admin = CrearUsuario("Admin", Usuario.RolAdministrador, pin: "2580");
        var cajero = CrearUsuario("Cajero", Usuario.RolCajero, pin: "1470");
        var servicio = new UsuarioService();

        Assert.True(servicio.Autorizar(admin.IdUsuario, "2580").Exitoso);
        Assert.False(servicio.Autorizar(admin.IdUsuario, "0000").Exitoso);
        Assert.False(servicio.Autorizar(cajero.IdUsuario, "1470").Exitoso);
    }

    [Fact]
    public void CajaYCompras_RegistranQuienLasHizo()
    {
        var admin = CrearUsuario("Admin", Usuario.RolAdministrador);
        Sesion.Iniciar(admin);

        var caja = AbrirCaja(5m);
        new CajaService().CerrarCaja(caja.IdCaja, 5m);
        var cerrada = new CajaService().ObtenerResumen(caja.IdCaja).Caja;

        Assert.Equal("Admin", cerrada.NombreUsuarioApertura);
        Assert.Equal("Admin", cerrada.NombreUsuarioCierre);
    }
}
