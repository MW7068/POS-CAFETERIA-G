namespace PosCafeteria.Dominio;

public class ConfiguracionNegocio
{
    public int Id { get; set; } = 1;
    public string NombreNegocio { get; set; } = "Café de Mamá Oli";
    public string? RucOpcional { get; set; }
    public string? DireccionOpcional { get; set; }
    public string? TelefonoOpcional { get; set; }

    public decimal TasaImpuestoActiva { get; set; } = 18.00m;
    public bool Tasa18Habilitada { get; set; } = true;
    public bool Tasa105Habilitada { get; set; } = true;
    public decimal LimiteDescuentoCajero { get; set; } = 0.00m;
    public decimal LimiteDescuentoAdministrador { get; set; } = 100.00m;
    public string SerieComprobanteActiva { get; set; } = "INT-001";

    public ConfiguracionNegocio() { }

    public void ConfigurarTasas(bool habilitar18, bool habilitar105, decimal tasaSeleccionada)
    {
        if (!habilitar18 && !habilitar105)
            throw new InvalidOperationException("Debe haber al menos una tasa interna habilitada.");

        if (tasaSeleccionada != 18.00m && tasaSeleccionada != 10.50m)
            throw new ArgumentException("Solo se admiten las tasas internas configurables de 18% o 10.5%.");

        if ((tasaSeleccionada == 18.00m && !habilitar18) || (tasaSeleccionada == 10.50m && !habilitar105))
            throw new InvalidOperationException("La tasa seleccionada debe estar habilitada.");

        Tasa18Habilitada = habilitar18;
        Tasa105Habilitada = habilitar105;
        TasaImpuestoActiva = tasaSeleccionada;
    }

    public void ConfigurarLimitesDescuento(decimal limiteCajero, decimal limiteAdmin)
    {
        if (limiteCajero < 0 || limiteCajero > 100 || limiteAdmin < 0 || limiteAdmin > 100)
            throw new ArgumentOutOfRangeException("Los límites de descuento deben estar entre 0 y 100.");

        LimiteDescuentoCajero = decimal.Round(limiteCajero, 2, MidpointRounding.AwayFromZero);
        LimiteDescuentoAdministrador = decimal.Round(limiteAdmin, 2, MidpointRounding.AwayFromZero);
    }
}