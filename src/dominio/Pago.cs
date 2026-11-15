using System;

namespace PosCafeteria.Dominio;

public class Pago
{
    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int TurnoCajaId { get; private set; }
    public MetodoPago Metodo { get; private set; }
    public decimal MontoTotal { get; private set; }
    public decimal MontoRecibido { get; private set; }

    public decimal Vuelto => Metodo == MetodoPago.Efectivo ? MontoRecibido - MontoTotal : 0m;

    private Pago() { }

    public static Pago RegistrarEfectivo(int ventaId, int turnoCajaId, decimal total, decimal recibido)
    {
        if (recibido < total) throw new InvalidOperationException("Efectivo insuficiente.");
        return new Pago
        {
            VentaId = ventaId,
            TurnoCajaId = turnoCajaId,
            Metodo = MetodoPago.Efectivo,
            MontoTotal = decimal.Round(total, 2, MidpointRounding.AwayFromZero),
            MontoRecibido = decimal.Round(recibido, 2, MidpointRounding.AwayFromZero)
        };
    }

    public static Pago RegistrarDigital(int ventaId, int turnoCajaId, MetodoPago metodo, decimal total, bool confirmado)
    {
        if (metodo != MetodoPago.YapePlin && metodo != MetodoPago.Tarjeta) throw new ArgumentException("Método inválido.");
        if (!confirmado) throw new InvalidOperationException("Falta confirmación manual.");

        decimal monto = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        return new Pago { VentaId = ventaId, TurnoCajaId = turnoCajaId, Metodo = metodo, MontoTotal = monto, MontoRecibido = monto };
    }

    public static Pago RegistrarSinCobro(int ventaId, int turnoCajaId, RolUsuario rol)
    {
        if (rol != RolUsuario.Administrador) throw new UnauthorizedAccessException("Requiere administrador.");
        return new Pago { VentaId = ventaId, TurnoCajaId = turnoCajaId, Metodo = MetodoPago.SinCobroAutorizado };
    }
}