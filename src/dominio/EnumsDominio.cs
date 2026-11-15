using System;

namespace PosCafeteria.Dominio;

public class MovimientoCaja
{
    public int Id { get; private set; }
    public int TurnoCajaId { get; private set; }
    public TipoMovimientoCaja Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public string Concepto { get; private set; } = string.Empty;
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;

    private MovimientoCaja() { }

    public static MovimientoCaja Crear(int turnoCajaId, TipoMovimientoCaja tipo, decimal monto, string concepto)
    {
        if (turnoCajaId <= 0 || monto <= 0m || string.IsNullOrWhiteSpace(concepto))
            throw new ArgumentException("Movimiento de caja inválido.");

        return new MovimientoCaja
        {
            TurnoCajaId = turnoCajaId,
            Tipo = tipo,
            Monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero),
            Concepto = concepto.Trim(),
            FechaUtc = DateTime.UtcNow
        };
    }
}