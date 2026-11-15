using System;

namespace PosCafeteria.Dominio;

public class MovimientoCaja
{
    public int Id { get; private set; }
    public int TurnoCajaId { get; private set; }
    public TipoMovimientoCaja Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public string Concepto { get; private set; } = string.Empty;
    public int? VentaId { get; private set; }
    public string? ReferenciaExterna { get; private set; }
    public int UsuarioId { get; private set; }
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;

    private MovimientoCaja() { }

    public static MovimientoCaja Crear(
        int turnoCajaId,
        TipoMovimientoCaja tipo,
        decimal monto,
        string concepto,
        int usuarioId,
        int? ventaId = null,
        string? referenciaExterna = null)
    {
        if (turnoCajaId <= 0)
            throw new ArgumentException("Se requiere un turno de caja válido.", nameof(turnoCajaId));

        if (monto <= 0m || decimal.Round(monto, 2, MidpointRounding.AwayFromZero) != monto)
            throw new ArgumentException("El monto debe ser mayor a cero con máximo dos decimales.", nameof(monto));

        if (string.IsNullOrWhiteSpace(concepto))
            throw new ArgumentException("El concepto del movimiento es obligatorio.", nameof(concepto));

        if (usuarioId <= 0)
            throw new ArgumentException("Se requiere un usuario válido.", nameof(usuarioId));

        if (tipo == TipoMovimientoCaja.ReembolsoDigital && string.IsNullOrWhiteSpace(referenciaExterna))
            throw new ArgumentException("El reembolso digital/tarjeta requiere documentar la referencia de validación externa.", nameof(referenciaExterna));

        return new MovimientoCaja
        {
            TurnoCajaId = turnoCajaId,
            Tipo = tipo,
            Monto = monto,
            Concepto = concepto.Trim(),
            VentaId = ventaId,
            ReferenciaExterna = string.IsNullOrWhiteSpace(referenciaExterna) ? null : referenciaExterna.Trim(),
            UsuarioId = usuarioId,
            FechaUtc = DateTime.UtcNow
        };
    }
}