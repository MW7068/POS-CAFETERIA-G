using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioComprobantes : IComprobanteServicio
{
    private readonly IAlmacenPos _almacen;

    public ServicioComprobantes(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<ResultadoOperacion<ComprobanteDto>> ObtenerComprobanteAsync(int ventaId)
    {
        if (ventaId <= 0)
            return ResultadoOperacion<ComprobanteDto>.Fallo(CodigosErrorPos.Validacion, "El identificador de venta es inválido.");

        var venta = await _almacen.ObtenerVentaPorIdAsync(ventaId);
        if (venta is null)
            return ResultadoOperacion<ComprobanteDto>.Fallo(CodigosErrorPos.NoEncontrado, "La venta indicada no existe.");

        var comprobante = await _almacen.ObtenerComprobantePorVentaIdAsync(ventaId);
        if (comprobante is null)
            return ResultadoOperacion<ComprobanteDto>.Fallo(CodigosErrorPos.NoEncontrado, "La venta aún no tiene un comprobante emitido.");

        string estado = venta.Estado == EstadoVenta.Anulada ? "ANULADO" : "EMITIDO";
        string detalleItems = string.Join(
            " | ",
            venta.Detalles.Select(d => $"{d.Cantidad}x {d.NombreProducto} (S/ {d.Subtotal.ToString("0.00", CultureInfo.InvariantCulture)})"));

        string resumen = $"{Comprobante.Leyenda} | {comprobante.NumeroFormateado} | Cliente: {comprobante.ClienteNombre} | {detalleItems} | Total: S/ {comprobante.Total.ToString("0.00", CultureInfo.InvariantCulture)}";

        var dto = new ComprobanteDto(
            comprobante.Idcomprobante,
            comprobante.Serie,
            comprobante.Numero,
            Comprobante.Leyenda,
            comprobante.FechaEmisionUtc,
            estado,
            comprobante.Total,
            resumen);

        return ResultadoOperacion<ComprobanteDto>.Ok(dto);
    }
}