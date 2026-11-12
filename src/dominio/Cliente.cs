// hecho por alex

namespace PosCafeteria.Dominio;

public class Cliente
{
    // Identificador generado automáticamente por la base de datos.
    public int Id { get; private set; }

    // Nombre obligatorio según la configuración del contexto.
    public string Nombre { get; set; } = string.Empty;

    // Celular opcional del cliente.
    public string? Celular { get; set; }
}