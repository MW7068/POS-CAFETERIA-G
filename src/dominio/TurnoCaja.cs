using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class TurnoCaja
{
    public int Id { get; private set; }
    public EstadoTurno Estado { get; private set; } = EstadoTurno.Abierto;

    public int UsuarioAperturaId { get; private set; }
    public DateTime FechaAperturaUtc { get; private set; } = DateTime.UtcNow;
    public decimal MontoApertura { get; private set; }

    public int? UsuarioCierreId { get; private set; }
    public DateTime? FechaCierreUtc { get; private set; }
    public decimal? EfectivoEsperadoCierre { get; private set; }
    public decimal? EfectivoContadoCierre { get; private set; }
    public decimal? DiferenciaCierre { get; private set; }
    public string? ObservacionCierre { get; private set; }

    private TurnoCaja() { }

    public static TurnoCaja Abrir(int usuarioAperturaId, decimal montoApertura)
    {
        if (usuarioAperturaId <= 0)
            throw new ArgumentException("Se requiere un usuario válido para abrir turno.", nameof(usuarioAperturaId));

        if (montoApertura < 0m || decimal.Round(montoApertura, 2, MidpointRounding.AwayFromZero) != montoApertura)
            throw new ArgumentException("El monto de apertura debe ser no negativo con máximo dos decimales.", nameof(montoApertura));

        return new TurnoCaja
        {
            Estado = EstadoTurno.Abierto,
            UsuarioAperturaId = usuarioAperturaId,
            FechaAperturaUtc = DateTime.UtcNow,
            MontoApertura = montoApertura
        };
    }

    public decimal CalcularEfectivoEsperado(IEnumerable<MovimientoCaja> movimientos)
    {
        ArgumentNullException.ThrowIfNull(movimientos);

        decimal ventasEfectivo = 0m;
        decimal propinasEfectivo = 0m;
        decimal ingresosManuales = 0m;
        decimal egresosManuales = 0m;
        decimal reembolsosEfectivo = 0m;

        foreach (var mov in movimientos.Where(m => m.TurnoCajaId == Id))
        {
            switch (mov.Tipo)
            {
                case TipoMovimientoCaja.VentaEfectivo:
                    ventasEfectivo += mov.Monto;
                    break;
                case TipoMovimientoCaja.PropinaEfectivo:
                    propinasEfectivo += mov.Monto;
                    break;
                case TipoMovimientoCaja.IngresoManual:
                    ingresosManuales += mov.Monto;
                    break;
                case TipoMovimientoCaja.EgresoManual:
                    egresosManuales += mov.Monto;
                    break;
                case TipoMovimientoCaja.ReembolsoEfectivo:
                    reembolsosEfectivo += mov.Monto;
                    break;
                case TipoMovimientoCaja.ReembolsoDigital:
                    break;
            }
        }

        return MontoApertura
               + ventasEfectivo
               + propinasEfectivo
               + ingresosManuales
               - egresosManuales
               - reembolsosEfectivo;
    }

    public void Cerrar(
        int usuarioCierreId,
        decimal efectivoContado,
        IEnumerable<MovimientoCaja> movimientos,
        string? observacion = null)
    {
        if (Estado == EstadoTurno.Cerrado)
            throw new InvalidOperationException("El turno ya está cerrado; un cierre es irrepetible e inmutable.");

        if (usuarioCierreId <= 0)
            throw new ArgumentException("Se requiere un usuario responsable para cerrar el turno.", nameof(usuarioCierreId));

        if (efectivoContado < 0m || decimal.Round(efectivoContado, 2, MidpointRounding.AwayFromZero) != efectivoContado)
            throw new ArgumentException("El efectivo contado debe ser no negativo con máximo dos decimales.", nameof(efectivoContado));

        decimal esperado = CalcularEfectivoEsperado(movimientos);

        Estado = EstadoTurno.Cerrado;
        UsuarioCierreId = usuarioCierreId;
        FechaCierreUtc = DateTime.UtcNow;
        EfectivoEsperadoCierre = esperado;
        EfectivoContadoCierre = efectivoContado;
        DiferenciaCierre = efectivoContado - esperado;
        ObservacionCierre = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
    }
}