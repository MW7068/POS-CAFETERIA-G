using System;

namespace PosCafeteria.Dominio;

public class Pago
{
    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int CajaId { get; private set; }
    public int UsuarioId { get; private set; }
    public MetodoPago Metodo { get; private set; }
    public decimal MontoConsumo { get; private set; }
    public decimal MontoPropina { get; private set; }
    public decimal MontoTotal { get; private set; }
    public decimal MontoRecibido { get; private set; }
    public bool ConfirmacionManualDigital { get; private set; }
    public string? ReferenciaOperacionDigital { get; private set; }
    public DateTime FechaPagoUtc { get; private set; } = DateTime.UtcNow;

    public decimal Vuelto => Metodo == MetodoPago.Efectivo ? MontoRecibido - MontoTotal : 0m;

    private Pago() { }

    public static Pago RegistrarEfectivo(int ventaId, int cajaId, int usuarioId, decimal consumo, decimal propina, decimal recibido)
    {
        if (consumo < 0m || propina < 0m) throw new ArgumentException("Consumo o propina inválidos.");
        decimal c = decimal.Round(consumo, 2, MidpointRounding.AwayFromZero);
        decimal p = decimal.Round(propina, 2, MidpointRounding.AwayFromZero);
        decimal total = c + p;
        decimal r = decimal.Round(recibido, 2, MidpointRounding.AwayFromZero);

        if (r < total) throw new InvalidOperationException("El efectivo recibido es menor al total a pagar.");

        return new Pago
        {
            VentaId = ventaId,
            CajaId = cajaId,
            UsuarioId = usuarioId,
            Metodo = MetodoPago.Efectivo,
            MontoConsumo = c,
            MontoPropina = p,
            MontoTotal = total,
            MontoRecibido = r,
            ConfirmacionManualDigital = false,
            FechaPagoUtc = DateTime.UtcNow
        };
    }

    public static Pago RegistrarDigital(
        int ventaId,
        int cajaId,
        int usuarioId,
        MetodoPago metodo,
        decimal consumo,
        decimal propina,
        bool confirmacionManual,
        string? referenciaOperacion = null)
    {
        if (metodo != MetodoPago.YapePlin && metodo != MetodoPago.Tarjeta)
            throw new ArgumentException("Método digital inválido.", nameof(metodo));
        if (!confirmacionManual)
            throw new InvalidOperationException("Se requiere confirmación manual del pago digital.");
        if (consumo < 0m || propina < 0m)
            throw new ArgumentException("Consumo o propina inválidos.");

        decimal c = decimal.Round(consumo, 2, MidpointRounding.AwayFromZero);
        decimal p = decimal.Round(propina, 2, MidpointRounding.AwayFromZero);
        decimal total = c + p;

        return new Pago
        {
            VentaId = ventaId,
            CajaId = cajaId,
            UsuarioId = usuarioId,
            Metodo = metodo,
            MontoConsumo = c,
            MontoPropina = p,
            MontoTotal = total,
            MontoRecibido = total,
            ConfirmacionManualDigital = true,
            ReferenciaOperacionDigital = string.IsNullOrWhiteSpace(referenciaOperacion) ? null : referenciaOperacion.Trim(),
            FechaPagoUtc = DateTime.UtcNow
        };
    }

    public static Pago RegistrarSinCobro(int ventaId, int cajaId, int usuarioId, RolUsuario rolAutorizador)
    {
        if (rolAutorizador != RolUsuario.Administrador)
            throw new UnauthorizedAccessException("Solo un administrador puede autorizar una venta sin cobro.");

        return new Pago
        {
            VentaId = ventaId,
            CajaId = cajaId,
            UsuarioId = usuarioId,
            Metodo = MetodoPago.SinCobroAutorizado,
            MontoConsumo = 0m,
            MontoPropina = 0m,
            MontoTotal = 0m,
            MontoRecibido = 0m,
            ConfirmacionManualDigital = false,
            FechaPagoUtc = DateTime.UtcNow
        };
    }
}