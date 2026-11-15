namespace PosCafeteria.Dominio;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string AlgoritmoHash { get; set; } = "PBKDF2-SHA256";
    public int IteracionesHash { get; set; } = 100_000;
    public bool Activo { get; set; } = true;
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;

    public Usuario() { }

    public Usuario(
        string nombreUsuario,
        string nombreCompleto,
        RolUsuario rol,
        string passwordHash,
        string passwordSalt,
        string algoritmoHash = "PBKDF2-SHA256",
        int iteracionesHash = 100_000)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(nombreUsuario));
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.", nameof(nombreCompleto));
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
            throw new ArgumentException("El hash y la sal de contraseña son obligatorios.");

        NombreUsuario = nombreUsuario.Trim().ToLowerInvariant();
        NombreCompleto = nombreCompleto.Trim();
        Rol = rol;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        AlgoritmoHash = algoritmoHash;
        IteracionesHash = iteracionesHash;
        Activo = true;
        CreadoUtc = DateTime.UtcNow;
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
}