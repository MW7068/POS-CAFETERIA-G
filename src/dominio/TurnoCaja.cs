using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class TurnoCaja
{
    public int Id { get; private set; }
    public EstadoTurno Estado { get; private set; } = EstadoTurno.Abierto;
    public int UsuarioId { get; private set; }
    public DateTime FechaAperturaUtc { get; private set; } = DateTime.UtcNow;
    public decimal MontoApertura { get; private set; }
    public decimal? EfectivoContado { get; private set; }

    private TurnoCaja() { }

    public static TurnoCaja Abrir(int usuarioId, decimal montoApertura)
    {
        if (usuarioId <= 0 || montoApertura < 0m) throw new ArgumentException("Apertura inválida.");
        return new TurnoCaja
        {
            UsuarioId = usuarioId,
            MontoApertura = decimal.Round(montoApertura, 2, MidpointRounding.AwayFromZero)
        };
    }

    public decimal CalcularEfectivoEsperado(IEnumerable<MovimientoCaja> movimientos)
    {
        var delTurno = movimientos.Where(m => m.TurnoCajaId == Id);
        decimal ingresos = delTurno.Where(m => m.Tipo is TipoMovimientoCaja.VentaEfectivo or TipoMovimientoCaja.IngresoManual).Sum(m => m.Monto);
        decimal egresos = delTurno.Where(m => m.Tipo is TipoMovimientoCaja.EgresoManual or TipoMovimientoCaja.ReembolsoEfectivo).Sum(m => m.Monto);
        return MontoApertura + ingresos - egresos;
    }

    public void Cerrar(decimal efectivoContado)
    {
        if (Estado == EstadoTurno.Cerrado || efectivoContado < 0m) throw new InvalidOperationException("No se puede cerrar el turno.");
        Estado = EstadoTurno.Cerrado;
        EfectivoContado = decimal.Round(efectivoContado, 2, MidpointRounding.AwayFromZero);
    }
}