using System;

namespace PosCafeteria.Dominio;

public class ComprobanteInterno
{
    public const string Leyenda = "COMPROBANTE INTERNO - SIN VALIDEZ TRIBUTARIA";

    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public string Serie { get; private set; } = "INT-001";
    public int Numero { get; private set; }
    public string ClienteNombre { get; private set; } = "Público general";
    public decimal Total { get; private set; }

    public string NumeroCompleto => $"{Serie}-{Numero:D6}";

    private ComprobanteInterno() { }

    public static ComprobanteInterno Emitir(Venta venta, Pago pago, string clienteNombre, string serie, int numero)
    {
        if (venta.Estado != EstadoVenta.Confirmada || numero <= 0)
            throw new InvalidOperationException("No se puede emitir comprobante.");

        return new ComprobanteInterno
        {
            VentaId = venta.Id,
            Serie = serie.Trim().ToUpperInvariant(),
            Numero = numero,
            ClienteNombre = string.IsNullOrWhiteSpace(clienteNombre) ? "Público general" : clienteNombre.Trim(),
            Total = pago.MontoTotal
        };
    }
}