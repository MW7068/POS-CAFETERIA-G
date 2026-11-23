using System;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioCaja : ICajaServicio
{
    private readonly IAlmacenPos _almacen;

    public ServicioCaja(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<ResultadoOperacion<int>> AbrirTurnoAsync(AbrirTurnoRequest request)
    {
        if (request is null)
            return ResultadoOperacion<int>.Fallo(CodigosErrorPos.Validacion, "La solicitud es obligatoria.");

        if (request.MontoApertura < 0m)
            return ResultadoOperacion<int>.Fallo(CodigosErrorPos.Validacion, "El monto de apertura no puede ser negativo.");

        var usuario = await _almacen.ObtenerUsuarioPorIdAsync(request.UsuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion<int>.Fallo(CodigosErrorPos.NoAutorizado, "Usuario no válido o inactivo.");

        var cajaAbierta = await _almacen.ObtenerCajaAbiertaPorUsuarioAsync(request.UsuarioId);
        if (cajaAbierta is not null)
            return ResultadoOperacion<int>.Fallo(CodigosErrorPos.TurnoYaAbierto, "El usuario ya tiene un turno de caja abierto.");

        var nuevaCaja = Caja.Abrir(request.UsuarioId, request.MontoApertura);
        await _almacen.EjecutarEnTransaccionAsync(async () =>
        {
            await _almacen.AgregarCajaAsync(nuevaCaja);
        });

        return ResultadoOperacion<int>.Ok(nuevaCaja.Id);
    }

    public async Task<ResultadoOperacion> RegistrarMovimientoCajaAsync(RegistrarMovimientoCajaRequest request)
    {
        if (request is null)
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "La solicitud es obligatoria.");

        if (request.Monto <= 0m)
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "El monto debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "El motivo del movimiento es obligatorio.");

        if (!TryParseMovimientoManual(request.TipoMovimiento, out var tipo, out var origen))
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "Tipo de movimiento de caja no válido.");

        var usuario = await _almacen.ObtenerUsuarioPorIdAsync(request.UsuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion.Fallo(CodigosErrorPos.NoAutorizado, "Usuario no válido o inactivo.");

        var caja = await _almacen.ObtenerCajaPorIdAsync(request.TurnoId);
        if (caja is null || caja.Estado != EstadoCaja.Abierta)
            return ResultadoOperacion.Fallo(CodigosErrorPos.TurnoCerradoOInexistente, "El turno de caja no existe o ya está cerrado.");

        var movimiento = CajaMovimiento.Crear(
            caja.Id,
            usuario.Id,
            tipo,
            origen,
            MetodoPago.Efectivo,
            request.Monto,
            request.Motivo);

        await _almacen.EjecutarEnTransaccionAsync(async () =>
        {
            await _almacen.AgregarMovimientoCajaAsync(movimiento);
        });

        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion<ResumenCierreDto>> CerrarTurnoAsync(CerrarTurnoRequest request)
    {
        if (request is null)
            return ResultadoOperacion<ResumenCierreDto>.Fallo(CodigosErrorPos.Validacion, "La solicitud es obligatoria.");

        if (request.EfectivoContado < 0m)
            return ResultadoOperacion<ResumenCierreDto>.Fallo(CodigosErrorPos.Validacion, "El efectivo contado no puede ser negativo.");

        var usuario = await _almacen.ObtenerUsuarioPorIdAsync(request.UsuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion<ResumenCierreDto>.Fallo(CodigosErrorPos.NoAutorizado, "Usuario no válido o inactivo.");

        var caja = await _almacen.ObtenerCajaPorIdAsync(request.TurnoId);
        if (caja is null)
            return ResultadoOperacion<ResumenCierreDto>.Fallo(CodigosErrorPos.TurnoCerradoOInexistente, "El turno de caja no existe.");

        if (caja.Estado == EstadoCaja.Cerrada)
            return ResultadoOperacion<ResumenCierreDto>.Fallo(CodigosErrorPos.EstadoInvalido, "La caja ya fue cerrada previamente.");

        var movimientos = await _almacen.ListarMovimientosCajaAsync(caja.Id);

        decimal ingresosDigitales = movimientos
            .Where(m => m.CajaId == caja.Id && m.MetodoPago != MetodoPago.Efectivo && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);

        decimal egresosDigitales = movimientos
            .Where(m => m.CajaId == caja.Id && m.MetodoPago != MetodoPago.Efectivo && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);

        decimal totalDigitalFueraGaveta = decimal.Round(ingresosDigitales - egresosDigitales, 2, MidpointRounding.AwayFromZero);

        await _almacen.EjecutarEnTransaccionAsync(() =>
        {
            caja.Cerrar(request.EfectivoContado, movimientos);
            return Task.CompletedTask;
        });

        var resumen = new ResumenCierreDto(
            caja.Id,
            caja.TotalEsperado ?? 0m,
            caja.MontoContado ?? 0m,
            caja.Diferencia ?? 0m,
            totalDigitalFueraGaveta,
            caja.FechaCierreUtc ?? DateTime.UtcNow);

        return ResultadoOperacion<ResumenCierreDto>.Ok(resumen);
    }

    public async Task<ResultadoOperacion> AnularVentaAsync(AnularVentaRequest request)
    {
        if (request is null || request.IdentificadorOperacion == Guid.Empty)
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "El identificador de operación es obligatorio.");

        var auth = await ValidadorAutorizacionAdmin.ValidarAsync(_almacen, request.Autorizacion);
        if (!auth.Exitoso || auth.Valor is null)
            return ResultadoOperacion.Fallo(auth.Error!.Codigo, auth.Error.Mensaje);

        string claveOp = request.IdentificadorOperacion.ToString("D");
        int? ventaYaAnuladaPorOp = await _almacen.ObtenerVentaIdPorOperacionAnulacionAsync(claveOp);
        if (ventaYaAnuladaPorOp.HasValue)
        {
            if (ventaYaAnuladaPorOp.Value == request.VentaId)
                return ResultadoOperacion.Ok();

            return ResultadoOperacion.Fallo(
                CodigosErrorPos.OperacionDuplicada,
                "El identificador de operación ya fue utilizado en otra anulación.");
        }

        var venta = await _almacen.ObtenerVentaPorIdAsync(request.VentaId);
        if (venta is null)
            return ResultadoOperacion.Fallo(CodigosErrorPos.NoEncontrado, "La venta indicada no existe.");

        if (venta.Estado == EstadoVenta.Anulada)
            return ResultadoOperacion.Fallo(CodigosErrorPos.EstadoInvalido, "La venta ya se encuentra anulada.");

        if (venta.Estado != EstadoVenta.Confirmada)
            return ResultadoOperacion.Fallo(CodigosErrorPos.EstadoInvalido, "Solo se pueden anular ventas confirmadas.");

        var caja = await _almacen.ObtenerCajaPorIdAsync(request.TurnoActualId);
        if (caja is null || caja.Estado != EstadoCaja.Abierta)
            return ResultadoOperacion.Fallo(CodigosErrorPos.TurnoCerradoOInexistente, "Se requiere un turno de caja abierto para registrar la anulación.");

        var pago = await _almacen.ObtenerPagoPorVentaIdAsync(venta.Id);
        if (pago is not null && pago.MontoTotal > 0m && pago.Metodo != MetodoPago.Efectivo && !request.ReembolsoExternoVerificado)
        {
            return ResultadoOperacion.Fallo(
                CodigosErrorPos.Validacion,
                "Debe confirmar que el reembolso digital externo fue verificado antes de anular.");
        }

        var admin = auth.Valor;
        string motivoLimpio = request.Autorizacion.Motivo.Trim();

        await _almacen.EjecutarEnTransaccionAsync(async () =>
        {
            venta.Anular();

            foreach (var detalle in venta.Detalles)
            {
                var producto = await _almacen.ObtenerProductoPorIdAsync(detalle.ProductoId)
                    ?? throw new InvalidOperationException($"Producto #{detalle.ProductoId} no encontrado.");

                producto.ReponerStock(detalle.Cantidad);

                var movStock = MovimientoStock.Crear(
                    detalle.ProductoId,
                    admin.Id,
                    TipoMovimientoStock.EntradaProducto,
                    detalle.Cantidad,
                    $"Anulación venta #{venta.Id}: {motivoLimpio}",
                    venta.Id);

                await _almacen.AgregarMovimientoStockAsync(movStock);
            }

            if (pago is not null && pago.MontoTotal > 0m)
            {
                var movCaja = CajaMovimiento.Crear(
                    caja.Id,
                    admin.Id,
                    TipoMovimientoCaja.Egreso,
                    OrigenMovimientoCaja.Reembolso,
                    pago.Metodo,
                    pago.MontoTotal,
                    $"Reembolso por anulación de venta #{venta.Id}",
                    venta.Id,
                    null,
                    motivoLimpio);

                await _almacen.AgregarMovimientoCajaAsync(movCaja);
            }

            await _almacen.RegistrarOperacionAnulacionAsync(claveOp, venta.Id);
        });

        return ResultadoOperacion.Ok();
    }

    private static bool TryParseMovimientoManual(string? valor, out TipoMovimientoCaja tipo, out OrigenMovimientoCaja origen)
    {
        tipo = default;
        origen = default;
        if (string.IsNullOrWhiteSpace(valor)) return false;

        switch (valor.Trim().ToLowerInvariant())
        {
            case "ingreso":
            case "ingresomanual":
                tipo = TipoMovimientoCaja.Ingreso;
                origen = OrigenMovimientoCaja.IngresoManual;
                return true;
            case "gasto":
                tipo = TipoMovimientoCaja.Egreso;
                origen = OrigenMovimientoCaja.Gasto;
                return true;
            case "retiro":
            case "egreso":
                tipo = TipoMovimientoCaja.Egreso;
                origen = OrigenMovimientoCaja.Retiro;
                return true;
            default:
                return false;
        }
    }
}