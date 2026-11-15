using System;

namespace PosCafeteria.Dominio;

public class ComprobanteInterno
{
    public const string LeyendaSinValidezTributaria = "COMPROBANTE INTERNO - SIN VALIDEZ TRIBUTARIA";

    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public string Serie { get; private set; } = "INT-001";
    public int Numero { get; private set; }
    public string NumeroCompleto => $"{Serie}-{Numero:D6}";
    public string Leyenda { get; private set; } = LeyendaSinValidezTributaria;

    // Snapshot del negocio al momento de emitir
    public string NombreNegocioSnapshot { get; private set; } = string.Empty;
    public string? RucNegocioSnapshot { get; private set; }
    public string? DireccionNegocioSnapshot { get; private set; }
    public string? TelefonoNegocioSnapshot { get; private set; }

    // Snapshot de atención al cliente
    public string ClienteNombreSnapshot { get; private set; } = "Público general";
    public TipoDocumentoCliente ClienteDocumentoTipoSnapshot { get; private set; } = TipoDocumentoCliente.Ninguno;
    public string? ClienteDocumentoNumeroSnapshot { get; private set; }
    public ModalidadAtencion ModalidadSnapshot { get; private set; }

    // Snapshot monetario inmutable (no se recalcula al reimprimir)
    public decimal ConsumoBrutoSnapshot { get; private set; }
    public decimal PorcentajeDescuentoSnapshot { get; private set; }
    public decimal MontoDescuentoSnapshot { get; private set; }
    public decimal ConsumoNetoSnapshot { get; private set; }
    public decimal TasaImpuestoSnapshot { get; private set; }
    public decimal BaseImponibleSnapshot { get; private set; }
    public decimal MontoImpuestoSnapshot { get; private set; }
    public decimal PropinaSnapshot { get; private set; }
    public decimal TotalCobradoSnapshot { get; private set; }

    public MetodoPago MetodoPagoSnapshot { get; private set; }
    public decimal MontoRecibidoSnapshot { get; private set; }
    public decimal VueltoSnapshot { get; private set; }

    public DateTime FechaEmisionUtc { get; private set; } = DateTime.UtcNow;

    // Trazabilidad de anulación (PBI-05 + PBI-08)
    public bool Anulado { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public string? ReferenciaAnulacion { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }

    private ComprobanteInterno() { }

    public static ComprobanteInterno EmitirDesdeVenta(
        Venta venta,
        Pago pago,
        ConfiguracionNegocio config,
        string serie,
        int numeroCorrelativo)
    {
        ArgumentNullException.ThrowIfNull(venta);
        ArgumentNullException.ThrowIfNull(pago);
        ArgumentNullException.ThrowIfNull(config);

        if (venta.Estado != EstadoVenta.Confirmada)
            throw new InvalidOperationException("Solo se puede emitir comprobante interno para una venta confirmada.");

        if (string.IsNullOrWhiteSpace(serie))
            throw new ArgumentException("La serie interna es obligatoria.", nameof(serie));

        if (numeroCorrelativo <= 0)
            throw new ArgumentException("El número correlativo debe ser mayor a cero.", nameof(numeroCorrelativo));

        return new ComprobanteInterno
        {
            VentaId = venta.Id,
            Serie = serie.Trim().ToUpperInvariant(),
            Numero = numeroCorrelativo,
            Leyenda = LeyendaSinValidezTributaria,

            NombreNegocioSnapshot = config.NombreNegocio,
            RucNegocioSnapshot = config.RucOpcional,
            DireccionNegocioSnapshot = config.DireccionOpcional,
            TelefonoNegocioSnapshot = config.TelefonoOpcional,

            ClienteNombreSnapshot = venta.ClienteNombreSnapshot,
            ClienteDocumentoTipoSnapshot = venta.ClienteDocumentoTipoSnapshot,
            ClienteDocumentoNumeroSnapshot = venta.ClienteDocumentoNumeroSnapshot,
            ModalidadSnapshot = venta.Modalidad,

            ConsumoBrutoSnapshot = venta.ConsumoBruto,
            PorcentajeDescuentoSnapshot = venta.PorcentajeDescuento,
            MontoDescuentoSnapshot = venta.MontoDescuento,
            ConsumoNetoSnapshot = venta.ConsumoNeto,
            TasaImpuestoSnapshot = venta.TasaImpuestoAplicada,
            BaseImponibleSnapshot = venta.BaseImponible,
            MontoImpuestoSnapshot = venta.MontoImpuesto,
            PropinaSnapshot = venta.Propina,
            TotalCobradoSnapshot = pago.TotalCobrado,

            MetodoPagoSnapshot = pago.Metodo,
            MontoRecibidoSnapshot = pago.MontoRecibido,
            VueltoSnapshot = pago.Vuelto,

            FechaEmisionUtc = DateTime.UtcNow,
            Anulado = false
        };
    }

    public void MarcarAnulado(string motivo, string? referenciaAnulacion = null)
    {
        if (Anulado)
            throw new InvalidOperationException("El comprobante ya figura como anulado.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        Anulado = true;
        MotivoAnulacion = motivo.Trim();
        ReferenciaAnulacion = string.IsNullOrWhiteSpace(referenciaAnulacion) ? null : referenciaAnulacion.Trim();
        FechaAnulacionUtc = DateTime.UtcNow;
    }
}