using System;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoDocumentoCliente TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    public Cliente() { }

    public Cliente(string nombre, TipoDocumentoCliente tipo, string numeroDocumento, string? telefono = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

        string doc = numeroDocumento?.Trim() ?? string.Empty;
        bool digitos = doc.All(char.IsDigit);

        if (tipo == TipoDocumentoCliente.Dni && (!digitos || doc.Length != 8))
            throw new ArgumentException("El DNI debe tener 8 dígitos.", nameof(numeroDocumento));
        if (tipo == TipoDocumentoCliente.Ruc && (!digitos || doc.Length != 11))
            throw new ArgumentException("El RUC debe tener 11 dígitos.", nameof(numeroDocumento));

        Nombre = nombre.Trim();
        TipoDocumento = tipo;
        NumeroDocumento = doc;
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
    }
}