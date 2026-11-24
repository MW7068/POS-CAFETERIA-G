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
        if (venta.Estado != EstadoVenta.Confirmada)
            throw new InvalidOperationException("Solo se emite comprobante para ventas confirmadas.");
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