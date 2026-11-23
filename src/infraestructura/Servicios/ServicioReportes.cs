using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioReportes : IReporteServicio
{
    private readonly IAlmacenPos _almacen;

    public ServicioReportes(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<ResultadoOperacion<ReporteNegocioDto>> ConsultarReporteAsync(FiltroReporteRequest filtro)
    {
        if (filtro is null)
            return ResultadoOperacion<ReporteNegocioDto>.Fallo(CodigosErrorPos.Validacion, "El filtro de reporte es obligatorio.");

        DateTime inicioUtc = ConvertirLocalAUtc(filtro.FechaInicioLocal);
        DateTime finUtc = ConvertirLocalAUtc(filtro.FechaFinLocal);

        if (inicioUtc > finUtc)
            return ResultadoOperacion<ReporteNegocioDto>.Fallo(CodigosErrorPos.Validacion, "La fecha de inicio no puede ser mayor que la fecha de fin.");

        MetodoPago? metodoFiltro = null;
        if (!string.IsNullOrWhiteSpace(filtro.MetodoPago))
        {
            if (!TryParseMetodoPago(filtro.MetodoPago, out var parsed))
                return ResultadoOperacion<ReporteNegocioDto>.Fallo(CodigosErrorPos.Validacion, "Método de pago de filtro inválido.");
            metodoFiltro = parsed;
        }

        var ventas = await _almacen.ListarVentasAsync();
        var pagosPorVenta = (await _almacen.ListarPagosAsync())
            .GroupBy(p => p.VentaId)
            .ToDictionary(g => g.Key, g => g.First());

        var registros = new List<(Venta Venta, Pago? Pago)>();
        foreach (var v in ventas)
        {
            if (v.Estado != EstadoVenta.Confirmada && v.Estado != EstadoVenta.Anulada)
                continue;

            if (v.Fecha < inicioUtc || v.Fecha > finUtc)
                continue;

            if (filtro.TurnoId.HasValue && v.CajaId != filtro.TurnoId.Value)
                continue;

            if (filtro.ClienteId.HasValue && v.ClienteId != filtro.ClienteId.Value)
                continue;

            pagosPorVenta.TryGetValue(v.Id, out var pago);

            if (metodoFiltro.HasValue && (pago is null || pago.Metodo != metodoFiltro.Value))
                continue;

            registros.Add((v, pago));
        }

        decimal brutoConfirmado = decimal.Round(
            registros.Sum(r => r.Venta.Total),
            2,
            MidpointRounding.AwayFromZero);

        decimal reembolsos = decimal.Round(
            registros.Where(r => r.Venta.Estado == EstadoVenta.Anulada).Sum(r => r.Venta.Total),
            2,
            MidpointRounding.AwayFromZero);

        decimal neto = decimal.Round(brutoConfirmado - reembolsos, 2, MidpointRounding.AwayFromZero);

        var vigentes = registros
            .Where(r => r.Venta.Estado == EstadoVenta.Confirmada)
            .ToList();

        decimal propinas = decimal.Round(
            vigentes.Where(r => r.Pago is not null).Sum(r => r.Pago!.MontoPropina),
            2,
            MidpointRounding.AwayFromZero);

        int cantidadVentas = vigentes.Count;

        var porMetodoPago = vigentes
            .Where(r => r.Pago is not null)
            .GroupBy(r => r.Pago!.Metodo)
            .Select(g => new ResumenMetodoPagoDto(
                g.Key.ToString(),
                g.Count(),
                decimal.Round(g.Sum(x => x.Pago!.MontoTotal), 2, MidpointRounding.AwayFromZero)))
            .OrderByDescending(m => m.TotalRecaudado)
            .ToList();

        var productosMasVendidos = vigentes
            .SelectMany(r => r.Venta.Detalles)
            .GroupBy(d => d.ProductoId)
            .Select(g => new ProductoMasVendidoDto(
                g.Key,
                g.First().NombreProducto,
                g.Sum(d => d.Cantidad),
                decimal.Round(g.Sum(d => d.Subtotal), 2, MidpointRounding.AwayFromZero)))
            .OrderByDescending(p => p.UnidadesVendidas)
            .ThenByDescending(p => p.ImporteTotal)
            .ToList();

        var reporte = new ReporteNegocioDto(
            brutoConfirmado,
            reembolsos,
            neto,
            propinas,
            cantidadVentas,
            porMetodoPago,
            productosMasVendidos);

        return ResultadoOperacion<ReporteNegocioDto>.Ok(reporte);
    }

    private static DateTime ConvertirLocalAUtc(DateTime fecha)
    {
        return fecha.Kind switch
        {
            DateTimeKind.Utc => fecha,
            DateTimeKind.Local => fecha.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fecha, DateTimeKind.Local).ToUniversalTime()
        };
    }

    private static bool TryParseMetodoPago(string valor, out MetodoPago metodo)
    {
        metodo = default;
        switch (valor.Trim().ToLowerInvariant())
        {
            case "efectivo":
                metodo = MetodoPago.Efectivo;
                return true;
            case "yape":
            case "plin":
            case "yapeplin":
            case "yape/plin":
                metodo = MetodoPago.YapePlin;
                return true;
            case "tarjeta":
                metodo = MetodoPago.Tarjeta;
                return true;
            case "sincobro":
            case "sincobroautorizado":
                metodo = MetodoPago.SinCobroAutorizado;
                return true;
            default:
                return false;
        }
    }
}