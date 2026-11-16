using System;

namespace PosCafeteria.Dominio;

public class CajaMovimiento
{
    public int Id { get; private set; }
    public int CajaId { get; private set; }
    public int UsuarioId { get; private set; }
    public int? VentaId { get; private set; }
    public TipoMovimientoCaja Tipo { get; private set; }
    public string Concepto { get; private set; } = string.Empty;
    public MetodoPago MetodoPago { get; private set; }
    public decimal Monto { get; private set; }
    public string? Referencia { get; private set; }
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;
    public string? Observacion { get; private set; }
    public OrigenMovimientoCaja Origen { get; private set; }

    private CajaMovimiento() { }

    public static CajaMovimiento Crear(
        int cajaId,
        int usuarioId,
        TipoMovimientoCaja tipo,
        OrigenMovimientoCaja origen,
        MetodoPago metodoPago,
        decimal monto,
        string concepto,
        int? ventaId = null,
        string? referencia = null,
        string? observacion = null)
    {
        if (cajaId <= 0 || usuarioId <= 0)
            throw new ArgumentException("Caja o usuario inválido.");
        if (monto <= 0m)
            throw new ArgumentException("El monto del movimiento debe ser mayor que cero.", nameof(monto));
        if (string.IsNullOrWhiteSpace(concepto))
            throw new ArgumentException("El concepto es obligatorio.", nameof(concepto));
        if (metodoPago == MetodoPago.SinCobroAutorizado)
            throw new ArgumentException("Un movimiento monetario de caja no puede registrarse como SinCobroAutorizado.", nameof(metodoPago));

        return new CajaMovimiento
        {
            CajaId = cajaId,
            UsuarioId = usuarioId,
            VentaId = ventaId,
            Tipo = tipo,
            Origen = origen,
            MetodoPago = metodoPago,
            Monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero),
            Concepto = concepto.Trim(),
            Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim(),
            Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim(),
            FechaUtc = DateTime.UtcNow
        };
    }
}