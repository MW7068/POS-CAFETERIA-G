using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Caja
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public DateTime FechaAperturaUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? FechaCierreUtc { get; private set; }
    public decimal MontoInicial { get; private set; }
    public decimal? MontoContado { get; private set; }
    public decimal? TotalIngresosEfectivo { get; private set; }
    public decimal? TotalEgresosEfectivo { get; private set; }
    public decimal? TotalEsperado { get; private set; }
    public decimal? Diferencia { get; private set; }
    public EstadoCaja Estado { get; private set; } = EstadoCaja.Abierta;
    public string? Observacion { get; private set; }

    private Caja() { }

    public static Caja Abrir(int usuarioId, decimal montoInicial, string? observacion = null)
    {
        if (usuarioId <= 0) throw new ArgumentException("Usuario inválido.", nameof(usuarioId));
        if (montoInicial < 0m) throw new ArgumentException("El monto inicial no puede ser negativo.", nameof(montoInicial));

        return new Caja
        {
            UsuarioId = usuarioId,
            FechaAperturaUtc = DateTime.UtcNow,
            MontoInicial = decimal.Round(montoInicial, 2, MidpointRounding.AwayFromZero),
            Estado = EstadoCaja.Abierta,
            Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim()
        };
    }

    public (decimal IngresosEfectivo, decimal EgresosEfectivo, decimal Esperado) CalcularTotalesEfectivo(
        IEnumerable<CajaMovimiento> movimientos)
    {
        var enEfectivo = movimientos.Where(m => m.CajaId == Id && m.MetodoPago == MetodoPago.Efectivo).ToList();

        decimal ingresos = enEfectivo
            .Where(m => m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);

        decimal egresos = enEfectivo
            .Where(m => m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);

        decimal esperado = decimal.Round(MontoInicial + ingresos - egresos, 2, MidpointRounding.AwayFromZero);
        return (ingresos, egresos, esperado);
    }

    public void Cerrar(decimal montoContado, IEnumerable<CajaMovimiento> movimientos, string? observacion = null)
    {
        if (Estado == EstadoCaja.Cerrada)
            throw new InvalidOperationException("La caja ya fue cerrada y no puede modificarse.");
        if (montoContado < 0m)
            throw new ArgumentException("El monto contado no puede ser negativo.", nameof(montoContado));

        var (ingresos, egresos, esperado) = CalcularTotalesEfectivo(movimientos);
        decimal contado = decimal.Round(montoContado, 2, MidpointRounding.AwayFromZero);

        TotalIngresosEfectivo = ingresos;
        TotalEgresosEfectivo = egresos;
        TotalEsperado = esperado;
        MontoContado = contado;
        Diferencia = decimal.Round(contado - esperado, 2, MidpointRounding.AwayFromZero);
        FechaCierreUtc = DateTime.UtcNow;
        Estado = EstadoCaja.Cerrada;

        if (!string.IsNullOrWhiteSpace(observacion))
            Observacion = observacion.Trim();
    }
}