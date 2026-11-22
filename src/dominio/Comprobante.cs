using System;

namespace PosCafeteria.Dominio;

public class Comprobante
{
    public const string Leyenda = "COMPROBANTE INTERNO - SIN VALIDEZ TRIBUTARIA";

    public int Idcomprobante { get; private set; }
    public int VentaId { get; private set; }
    public string Serie { get; private set; } = "INT-001";
    public int Numero { get; private set; }
    public string ClienteNombre { get; private set; } = "Público general";
    public decimal Total { get; private set; }
    public DateTime FechaEmisionUtc { get; private set; } = DateTime.UtcNow;

    public string NumeroFormateado => $"{Serie}-{Numero:D6}";

    private Comprobante() { }

    public static Comprobante Emitir(Venta venta, Pago pago, string serie, int numero, string? clienteNombre = null)
    {
        ArgumentNullException.ThrowIfNull(venta);
        ArgumentNullException.ThrowIfNull(pago);

        if (venta.Estado != EstadoVenta.Confirmada)
            throw new InvalidOperationException("Solo se emite comprobante para ventas confirmadas.");
        if (venta.Id <= 0 || pago.VentaId <= 0)
            throw new InvalidOperationException("La venta y el pago deben tener un identificador válido mayor a cero.");
        if (pago.VentaId != venta.Id)
            throw new InvalidOperationException("El pago no corresponde a la venta indicada.");
        if (pago.MontoTotal != decimal.Round(pago.MontoConsumo + pago.MontoPropina, 2, MidpointRounding.AwayFromZero))
            throw new InvalidOperationException("Los importes del pago son incoherentes.");

        if (pago.Metodo == MetodoPago.SinCobroAutorizado)
        {
            if (venta.Total != 0m || pago.MontoConsumo != 0m || pago.MontoPropina != 0m || pago.MontoTotal != 0m || pago.MontoRecibido != 0m)
                throw new InvalidOperationException("SinCobroAutorizado solo se admite cuando el total de la venta y los importes del pago son cero.");
        }
        else if (pago.MontoConsumo != venta.Total)
        {
            throw new InvalidOperationException("El importe del pago no coincide con el total de la venta.");
        }

        if (string.IsNullOrWhiteSpace(serie) || numero <= 0)
            throw new ArgumentException("Serie o número de comprobante inválido.");

        return new Comprobante
        {
            VentaId = venta.Id,
            Serie = serie.Trim().ToUpperInvariant(),
            Numero = numero,
            ClienteNombre = string.IsNullOrWhiteSpace(clienteNombre) ? "Público general" : clienteNombre.Trim(),
            Total = pago.MontoTotal,
            FechaEmisionUtc = DateTime.UtcNow
        };
    }
}