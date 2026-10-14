using System.Security.Cryptography;
using System.Text;

namespace LacteosMadeline.Helpers;

/// <summary>
/// PIN de acceso de los usuarios. Nunca se guarda el PIN: se guarda una
/// huella PBKDF2-SHA256 con sal aleatoria por usuario, de modo que quien
/// copie la base de datos no puede leer los PIN.
/// </summary>
public static class Pin
{
    public const int LongitudMinima = 4;
    public const int LongitudMaxima = 6;

    private const int Iteraciones = 100_000;
    private const int BytesSal = 16;
    private const int BytesHuella = 32;

    public static (bool EsValido, string Mensaje) Validar(string? pin)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < LongitudMinima || pin.Length > LongitudMaxima || !pin.All(char.IsAsciiDigit))
        {
            return (false, $"El PIN debe tener entre {LongitudMinima} y {LongitudMaxima} números.");
        }

        if (pin.Distinct().Count() == 1)
        {
            return (false, "El PIN no puede ser un mismo número repetido (por ejemplo 1111).");
        }

        return (true, string.Empty);
    }

    public static (string Huella, string Sal) Generar(string pin)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal);
        return (Convert.ToBase64String(Calcular(pin, sal)), Convert.ToBase64String(sal));
    }

    public static bool Verificar(string pin, string huella, string sal)
    {
        try
        {
            var esperada = Convert.FromBase64String(huella);
            var calculada = Calcular(pin, Convert.FromBase64String(sal));
            return CryptographicOperations.FixedTimeEquals(esperada, calculada);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Calcular(string pin, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pin), sal, Iteraciones, HashAlgorithmName.SHA256, BytesHuella);
}
