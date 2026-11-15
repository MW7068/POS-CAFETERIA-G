using System;

namespace PosCafeteria.Dominio;

public class Pago
{
    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int TurnoCajaId { get; private set; }
    public int UsuarioCajeroId { get; private set; }
    public MetodoPago Metodo { get; private set; }

    public decimal MontoConsumo { get; private set; }
    public decimal MontoPropina { get; private set; }
    public decimal TotalCobrado => MontoConsumo + MontoPropina;

    public decimal MontoRecibido { get; private set; }
    public decimal Vuelto { get; private set; }

    public bool ConfirmacionManualDigital { get; private set; }
    public string? ReferenciaOperacionDigital { get; private set; }

    public DateTime FechaPagoUtc { get; private set; } = DateTime.UtcNow;
    public bool Anulado { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    private Pago() { }

    public static Pago RegistrarEfectivo(
        int ventaId,
        int turnoCajaId,
        int usuarioCajeroId,
        decimal montoConsumo,
        decimal montoPropina,
        decimal montoRecibido)
    {
        ValidarParametrosBase(ventaId, turnoCajaId, usuarioCajeroId, montoConsumo, montoPropina);

        decimal totalAPagar = montoConsumo + montoPropina;
        if (totalAPagar <= 0m)
            throw new InvalidOperationException("Use RegistrarSinCobroAutorizado cuando el total a pagar sea cero.");

        if (decimal.Round(montoRecibido, 2, MidpointRounding.AwayFromZero) != montoRecibido)
            throw new ArgumentException("El monto recibido no puede tener más de dos decimales.", nameof(montoRecibido));

        if (montoRecibido < totalAPagar)
            throw new InvalidOperationException($"Efectivo insuficiente. Total a pagar: S/ {totalAPagar:F2}, recibido: S/ {montoRecibido:F2}.");

        return new Pago
        {
            VentaId = ventaId,
            TurnoCajaId = turnoCajaId,
            UsuarioCajeroId = usuarioCajeroId,
            Metodo = MetodoPago.Efectivo,
            MontoConsumo = montoConsumo,
            MontoPropina = montoPropina,
            MontoRecibido = montoRecibido,
            Vuelto = montoRecibido - totalAPagar,
            ConfirmacionManualDigital = false,
            FechaPagoUtc = DateTime.UtcNow
        };
    }

    public static Pago RegistrarDigital(
        int ventaId,
        int turnoCajaId,
        int usuarioCajeroId,
        MetodoPago metodoDigital,
        decimal montoConsumo,
        decimal montoPropina,
        decimal montoRegistrado,
        bool confirmacionManual,
        string? referenciaOperacion = null)
    {
        if (metodoDigital != MetodoPago.YapePlin && metodoDigital != MetodoPago.Tarjeta)
            throw new ArgumentException("El canal digital debe ser Yape/Plin o Tarjeta.", nameof(metodoDigital));

        ValidarParametrosBase(ventaId, turnoCajaId, usuarioCajeroId, montoConsumo, montoPropina);

        decimal totalAPagar = montoConsumo + montoPropina;
        if (totalAPagar <= 0m)
            throw new InvalidOperationException("Una venta de importe cero no admite cobro digital.");

        if (!confirmacionManual)
            throw new InvalidOperationException("Todo cobro por Yape/Plin o Tarjeta requiere confirmación manual del cajero.");

        if (montoRegistrado != totalAPagar)
            throw new InvalidOperationException($"El importe digital ({montoRegistrado:F2}) debe coincidir exactamente con el total a pagar ({totalAPagar:F2}).");

        return new Pago
        {
            VentaId = ventaId,
            TurnoCajaId = turnoCajaId,
            UsuarioCajeroId = usuarioCajeroId,
            Metodo = metodoDigital,
            MontoConsumo = montoConsumo,
            MontoPropina = montoPropina,
            MontoRecibido = totalAPagar,
            Vuelto = 0.00m,
            ConfirmacionManualDigital = true,
            ReferenciaOperacionDigital = string.IsNullOrWhiteSpace(referenciaOperacion) ? null : referenciaOperacion.Trim(),
            FechaPagoUtc = DateTime.UtcNow
        };
    }

    public static Pago RegistrarSinCobroAutorizado(
        int ventaId,
        int turnoCajaId,
        int usuarioAdminId,
        RolUsuario rolActor)
    {
        if (rolActor != RolUsuario.Administrador)
            throw new UnauthorizedAccessException("Solo un administrador puede registrar una venta sin cobro.");

        ValidarParametrosBase(ventaId, turnoCajaId, usuarioAdminId, 0.00m, 0.00m);

        return new Pago
        {
            VentaId = ventaId,
            TurnoCajaId = turnoCajaId,
            UsuarioCajeroId = usuarioAdminId,
            Metodo = MetodoPago.SinCobroAutorizado,
            MontoConsumo = 0.00m,
            MontoPropina = 0.00m,
            MontoRecibido = 0.00m,
            Vuelto = 0.00m,
            ConfirmacionManualDigital = false,
            FechaPagoUtc = DateTime.UtcNow
        };
    }

    public void MarcarAnulado(string motivo)
    {
        if (Anulado)
            throw new InvalidOperationException("El pago ya fue marcado como anulado.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        Anulado = true;
        MotivoAnulacion = motivo.Trim();
        FechaAnulacionUtc = DateTime.UtcNow;
    }

    private static void ValidarParametrosBase(
        int ventaId,
        int turnoCajaId,
        int usuarioId,
        decimal montoConsumo,
        decimal montoPropina)
    {
        if (ventaId <= 0)
            throw new ArgumentException("Identificador de venta inválido.", nameof(ventaId));
        if (turnoCajaId <= 0)
            throw new ArgumentException("Identificador de turno inválido.", nameof(turnoCajaId));
        if (usuarioId <= 0)
            throw new ArgumentException("Identificador de usuario inválido.", nameof(usuarioId));
        if (montoConsumo < 0m || montoPropina < 0m)
            throw new ArgumentException("Los importes no pueden ser negativos.");
        if (montoConsumo == 0m && montoPropina > 0m)
            throw new InvalidOperationException("Una venta con consumo cero no admite propina.");
    }
}