namespace PosCafeteria.Dominio;

public class Negocio
{
    public int Id { get; set; }
    public string NombreNegocio { get; set; } = "Café de Mamá Oli";
    public string? Ruc { get; set; }
    public decimal TasaImpuesto { get; set; } = 18.00m;

    public Negocio() { }
}