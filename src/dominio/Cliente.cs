using System;
using System.Linq;
using System.Net.Mail;

namespace PosCafeteria.Dominio;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoDocumentoCliente TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;

    public Cliente() { }

    public Cliente(
        string nombre,
        TipoDocumentoCliente tipoDocumento,
        string numeroDocumento,
        string? telefono = null,
        string? direccion = null,
        string? email = null)
    {
        ActualizarDatos(nombre, tipoDocumento, numeroDocumento, telefono, direccion, email);
        Activo = true;
        CreadoUtc = DateTime.UtcNow;
    }

    public void ActualizarDatos(
        string nombre,
        TipoDocumentoCliente tipoDocumento,
        string numeroDocumento,
        string? telefono = null,
        string? direccion = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(nombre));

        ValidarDocumento(tipoDocumento, numeroDocumento);

        string? emailLimpio = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (emailLimpio != null && !EsEmailValido(emailLimpio))
            throw new ArgumentException("El formato del correo electrónico no es válido.", nameof(email));

        Nombre = nombre.Trim();
        TipoDocumento = tipoDocumento;
        NumeroDocumento = numeroDocumento.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
        Email = emailLimpio;
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;

    public static void ValidarDocumento(TipoDocumentoCliente tipo, string numero)
    {
        if (tipo == TipoDocumentoCliente.Ninguno)
            throw new ArgumentException("Un cliente registrado debe tener DNI o RUC.", nameof(tipo));

        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("El número de documento es obligatorio.", nameof(numero));

        string limpio = numero.Trim();
        bool soloDigitos = limpio.All(char.IsDigit);

        if (tipo == TipoDocumentoCliente.Dni && (!soloDigitos || limpio.Length != 8))
            throw new ArgumentException("El DNI debe contener exactamente 8 dígitos numéricos.", nameof(numero));

        if (tipo == TipoDocumentoCliente.Ruc && (!soloDigitos || limpio.Length != 11))
            throw new ArgumentException("El RUC debe contener exactamente 11 dígitos numéricos.", nameof(numero));
    }

    private static bool EsEmailValido(string email)
    {
        if (email.Contains(' '))
            return false;

        return MailAddress.TryCreate(email, out var parsed) && parsed.Address == email && email.Contains('.');
    }
}