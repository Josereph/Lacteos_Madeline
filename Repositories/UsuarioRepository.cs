using LacteosMadeline.Data;
using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Repositories;

/// <summary>
/// Acceso a datos para Usuarios. El PIN nunca se lee ni se escribe en
/// texto: solo su huella y su sal (ver <see cref="Pin"/>).
/// </summary>
public class UsuarioRepository
{
    private const string Columnas = "IdUsuario, Nombre, Rol, Estado";

    public bool ExisteAlguno()
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM Usuarios);";
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    public int Registrar(Usuario usuario, string pin)
    {
        var (huella, sal) = Pin.Generar(pin);

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Usuarios (Nombre, Rol, PinHuella, PinSal, Estado, FechaCreacion)
            VALUES ($nombre, $rol, $huella, $sal, $estado, $fecha);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nombre", usuario.Nombre);
        command.Parameters.AddWithValue("$rol", usuario.Rol);
        command.Parameters.AddWithValue("$huella", huella);
        command.Parameters.AddWithValue("$sal", sal);
        command.Parameters.AddWithValue("$estado", usuario.Estado ? 1 : 0);
        command.Parameters.AddWithValue("$fecha", Fechas.ParaBaseDatos(DateTime.Now));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Modificar(Usuario usuario)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Usuarios SET Nombre = $nombre, Rol = $rol, Estado = $estado WHERE IdUsuario = $id;";
        command.Parameters.AddWithValue("$nombre", usuario.Nombre);
        command.Parameters.AddWithValue("$rol", usuario.Rol);
        command.Parameters.AddWithValue("$estado", usuario.Estado ? 1 : 0);
        command.Parameters.AddWithValue("$id", usuario.IdUsuario);
        command.ExecuteNonQuery();
    }

    public void CambiarPin(int idUsuario, string pin)
    {
        var (huella, sal) = Pin.Generar(pin);

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Usuarios SET PinHuella = $huella, PinSal = $sal WHERE IdUsuario = $id;";
        command.Parameters.AddWithValue("$huella", huella);
        command.Parameters.AddWithValue("$sal", sal);
        command.Parameters.AddWithValue("$id", idUsuario);
        command.ExecuteNonQuery();
    }

    /// <summary>Devuelve el usuario si existe, está activo y el PIN es correcto.</summary>
    public Usuario? Verificar(int idUsuario, string pin)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columnas}, PinHuella, PinSal FROM Usuarios WHERE IdUsuario = $id AND Estado = 1;";
        command.Parameters.AddWithValue("$id", idUsuario);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        var usuario = Mapear(reader);
        return Pin.Verificar(pin, reader.GetString(4), reader.GetString(5)) ? usuario : null;
    }

    public Usuario? ObtenerPorId(int idUsuario)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columnas} FROM Usuarios WHERE IdUsuario = $id;";
        command.Parameters.AddWithValue("$id", idUsuario);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public List<Usuario> Listar(bool soloActivos)
    {
        var usuarios = new List<Usuario>();

        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columnas} FROM Usuarios WHERE ($soloActivos = 0 OR Estado = 1) ORDER BY Nombre;";
        command.Parameters.AddWithValue("$soloActivos", soloActivos ? 1 : 0);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            usuarios.Add(Mapear(reader));
        }

        return usuarios;
    }

    public bool ExisteNombre(string nombre, int idExcluir = 0)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Usuarios WHERE Nombre = $nombre AND IdUsuario <> $id;";
        command.Parameters.AddWithValue("$nombre", nombre);
        command.Parameters.AddWithValue("$id", idExcluir);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>Administradores activos sin contar a <paramref name="idExcluir"/>.</summary>
    public int ContarAdministradoresActivos(int idExcluir = 0)
    {
        using var connection = DatabaseConnection.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Usuarios WHERE Rol = 'Administrador' AND Estado = 1 AND IdUsuario <> $id;";
        command.Parameters.AddWithValue("$id", idExcluir);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static Usuario Mapear(SqliteDataReader reader) => new()
    {
        IdUsuario = reader.GetInt32(0),
        Nombre = reader.GetString(1),
        Rol = reader.GetString(2),
        Estado = reader.GetInt32(3) == 1
    };
}
