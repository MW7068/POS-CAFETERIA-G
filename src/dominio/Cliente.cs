namespace PosCafeteria.Dominio;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Celular { get; set; }
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}