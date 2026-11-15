using System;
using System.Security.Cryptography;

namespace PosCafeteria.Dominio;

public class Usuario
{
    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public RolUsuario Rol { get; private set; } = RolUsuario.Cajero;
    public bool Activo { get; private set; } = true;

    private Usuario() { }

    public static Usuario Crear(string nombreUsuario, string password, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El usuario es obligatorio.", nameof(nombreUsuario));

        var u = new Usuario { NombreUsuario = nombreUsuario.Trim().ToLowerInvariant(), Rol = rol };
        u.CambiarPassword(password);
        return u;
    }

    public void CambiarPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ArgumentException("Mínimo 6 caracteres.", nameof(password));

        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, 100_000, HashAlgorithmName.SHA256, 32);
        PasswordSalt = Convert.ToBase64String(sal);
        PasswordHash = Convert.ToBase64String(hash);
    }

    public bool VerificarPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(PasswordSalt)) return false;
        byte[] sal = Convert.FromBase64String(PasswordSalt);
        byte[] esperado = Convert.FromBase64String(PasswordHash);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, sal, 100_000, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(actual, esperado);
    }

    public void Desactivar() => Activo = false;
}