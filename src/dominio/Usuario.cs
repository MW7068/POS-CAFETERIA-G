using System;
using System.Security.Cryptography;

namespace PosCafeteria.Dominio;

public class Usuario
{
    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public RolUsuario Rol { get; private set; } = RolUsuario.Cajero;
    public bool Activo { get; private set; } = true;

    private Usuario() { }

    public static Usuario Crear(string nombreUsuario, string password, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(nombreUsuario));

        var u = new Usuario
        {
            NombreUsuario = nombreUsuario.Trim().ToLowerInvariant(),
            Rol = rol
        };
        u.CambiarPassword(password);
        return u;
    }

    public void CambiarPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.", nameof(password));

        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, 100_000, HashAlgorithmName.SHA256, 32);
        Password = $"{Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerificarPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(Password)) return false;

        var partes = Password.Split(':');
        if (partes.Length != 2) return false;

        try
        {
            byte[] sal = Convert.FromBase64String(partes[0]);
            byte[] esperado = Convert.FromBase64String(partes[1]);
            if (sal.Length == 0 || esperado.Length == 0) return false;

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, sal, 100_000, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(actual, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public void Desactivar() => Activo = false;
}