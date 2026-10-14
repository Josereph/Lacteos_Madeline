namespace LacteosMadeline.Helpers;

/// <summary>
/// Una operación se rechazó por una regla del negocio (existencia
/// insuficiente, caja cerrada, el dato cambió mientras se editaba…).
/// El mensaje está pensado para mostrarse tal cual al usuario.
///
/// Se usa en lugar de InvalidOperationException porque .NET y
/// Microsoft.Data.Sqlite también lanzan esa excepción por fallos técnicos,
/// y un fallo técnico nunca debe presentarse como, por ejemplo,
/// "existencia insuficiente".
/// </summary>
public sealed class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
