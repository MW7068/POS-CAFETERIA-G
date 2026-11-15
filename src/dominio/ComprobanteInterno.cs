using System;

namespace PosCafeteria.Dominio;

public class ComprobanteInterno
{
    public const string Leyenda = "COMPROBANTE INTERNO - SIN VALIDEZ TRIBUTARIA";

    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public string ClienteNombre { get; private set; } = "Público general";
    public decimal Total { get; private set; }
    public DateTime FechaEmisionUtc { get; private set; } = DateTime.UtcNow;

    public string NumeroTicket => $"#{Id:D6}";

    private ComprobanteInterno() { }

    public static ComprobanteInterno Emitir(Venta venta, Pago pago, string? clienteNombre = null)
    {
        if (venta.Estado != EstadoVenta.Confirmada)
            throw new InvalidOperationException("Solo se emite comprobante para ventas confirmadas.");

        return new ComprobanteInterno
        {
            VentaId = venta.Id,
            ClienteNombre = string.IsNullOrWhiteSpace(clienteNombre) ? "Público general" : clienteNombre.Trim(),
            Total = pago.MontoTotal,
            FechaEmisionUtc = DateTime.UtcNow
        };
    }
}