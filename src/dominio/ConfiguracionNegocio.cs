using System;

namespace PosCafeteria.Dominio;

public class ConfiguracionNegocio
{
    public int Id { get; set; }
    public string NombreNegocio { get; set; } = "Cafetería POS";
    public string? Ruc { get; set; }
    public decimal TasaImpuesto { get; set; } = 18.00m;
    public decimal LimiteDescuentoSinAdmin { get; set; } = 10.00m;
    public string SerieComprobante { get; set; } = "INT-001";

    public ConfiguracionNegocio() { }
}