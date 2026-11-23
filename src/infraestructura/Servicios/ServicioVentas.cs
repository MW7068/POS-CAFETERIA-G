using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioVentas : IVentaServicio
{
    private const string SerieInterna = "INT-001";
    private readonly IAlmacenPos _almacen;

    public ServicioVentas(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<ResultadoOperacion<BorradorDto>> GuardarBorradorAsync(GuardarBorradorRequest request)
    {
        if (request is null)
            return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.Validacion, "La solicitud es obligatoria.");

        var usuario = await _almacen.ObtenerUsuarioPorIdAsync(request.UsuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.NoAutorizado, "Usuario no válido o inactivo.");

        if (request.Lineas is null || request.Lineas.Count == 0)
            return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.Validacion, "El borrador debe tener al menos un producto.");

        Cliente? cliente = null;
        if (request.ClienteId.HasValue)
        {
            cliente = await _almacen.ObtenerClientePorIdAsync(request.ClienteId.Value);
            if (cliente is null)
                return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.NoEncontrado, "El cliente seleccionado no existe.");
        }

        var productosValidados = new List<(Producto Producto, int Cantidad, string? Observaciones)>();
        foreach (var linea in request.Lineas)
        {
            if (linea.Cantidad <= 0)
                return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.Validacion, "La cantidad de cada producto debe ser mayor a cero.");

            var producto = await _almacen.ObtenerProductoPorIdAsync(linea.ProductoId);
            if (producto is null)
                return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.NoEncontrado, $"El producto #{linea.ProductoId} no existe.");

            if (!producto.Activo)
                return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.EstadoInvalido, $"El producto '{producto.Nombre}' está inactivo.");

            productosValidados.Add((producto, linea.Cantidad, linea.Observaciones));
        }

        Venta venta;
        bool esNuevo = !request.BorradorId.HasValue || request.BorradorId.Value <= 0;

        if (esNuevo)
        {
            venta = new Venta(
                request.UsuarioId,
                request.EsParaLlevar ? ModalidadVenta.ParaLlevar : ModalidadVenta.Local);
        }
        else
        {
            var existente = await _almacen.ObtenerVentaPorIdAsync(request.BorradorId!.Value);
            if (existente is null)
                return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.NoEncontrado, "El borrador indicado no existe.");

            if (existente.Estado != EstadoVenta.Borrador)
                return ResultadoOperacion<BorradorDto>.Fallo(CodigosErrorPos.EstadoInvalido, "La venta ya no se encuentra en estado borrador.");

            venta = existente;
            venta.ConfigurarModalidad(request.EsParaLlevar ? ModalidadVenta.ParaLlevar : ModalidadVenta.Local);

            foreach (var detalleAnterior in venta.Detalles.ToList())
                venta.QuitarDetalle(detalleAnterior.ProductoId, detalleAnterior.Observacion);
        }

        venta.AsignarCliente(cliente?.Id);
        foreach (var item in productosValidados)
            venta.AgregarProducto(item.Producto, item.Cantidad, item.Observaciones);

        await _almacen.EjecutarEnTransaccionAsync(async () =>
        {
            if (esNuevo)
                await _almacen.AgregarVentaAsync(venta);
        });

        return ResultadoOperacion<BorradorDto>.Ok(
            MapearBorrador(venta, cliente?.Nombre, request.ObservacionesGenerales));
    }

    public async Task<ResultadoOperacion<IReadOnlyList<BorradorDto>>> ObtenerBorradoresAsync(int usuarioId)
    {
        if (usuarioId <= 0)
            return ResultadoOperacion<IReadOnlyList<BorradorDto>>.Fallo(CodigosErrorPos.Validacion, "Usuario inválido.");

        var borradores = await _almacen.ListarBorradoresPorUsuarioAsync(usuarioId);
        var lista = new List<BorradorDto>(borradores.Count);

        foreach (var b in borradores)
        {
            string? clienteNombre = null;
            if (b.ClienteId.HasValue)
            {
                var cliente = await _almacen.ObtenerClientePorIdAsync(b.ClienteId.Value);
                clienteNombre = cliente?.Nombre;
            }

            lista.Add(MapearBorrador(b, clienteNombre, null));
        }

        return ResultadoOperacion<IReadOnlyList<BorradorDto>>.Ok(lista);
    }

    public async Task<ResultadoOperacion<VentaConfirmadaDto>> ConfirmarVentaAsync(ConfirmarVentaRequest request)
    {
        if (request is null || request.Cobro is null)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "Los datos de confirmación y cobro son obligatorios.");

        if (request.IdentificadorOperacion == Guid.Empty)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "El identificador de operación es obligatorio.");

        if (!TryParseMetodoPago(request.Cobro.MetodoPago, out var metodoPago))
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "Método de pago no válido.");

        decimal descuentoPct = decimal.Round(request.Cobro.DescuentoPorcentaje, 2, MidpointRounding.AwayFromZero);
        decimal propina = decimal.Round(request.Cobro.Propina, 2, MidpointRounding.AwayFromZero);

        if (descuentoPct < 0m || descuentoPct > 100m)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "El porcentaje de descuento debe estar entre 0 y 100.");

        if (propina < 0m)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "La propina no puede ser negativa.");

        // 1. Verificación de idempotencia (#1)
        string claveOperacion = request.IdentificadorOperacion.ToString("D");
        var ventaPrevia = await _almacen.ObtenerVentaPorOperacionAsync(claveOperacion);
        if (ventaPrevia is not null)
        {
            var pagoPrevio = await _almacen.ObtenerPagoPorVentaIdAsync(ventaPrevia.Id);
            var compPrevio = await _almacen.ObtenerComprobantePorVentaIdAsync(ventaPrevia.Id);

            bool datosCoinciden =
                ventaPrevia.Id == request.BorradorId &&
                ventaPrevia.UsuarioId == request.UsuarioId &&
                ventaPrevia.CajaId == request.TurnoId &&
                ventaPrevia.Descuento == descuentoPct &&
                pagoPrevio is not null &&
                pagoPrevio.Metodo == metodoPago &&
                pagoPrevio.MontoPropina == propina;

            if (!datosCoinciden || pagoPrevio is null || compPrevio is null)
            {
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(
                    CodigosErrorPos.OperacionDuplicada,
                    "El identificador de operación ya fue utilizado con datos distintos.");
            }

            return ResultadoOperacion<VentaConfirmadaDto>.Ok(
                ConstruirDtoConfirmada(ventaPrevia, pagoPrevio, compPrevio));
        }

        // 2. Validación de usuario, turno y borrador
        var usuario = await _almacen.ObtenerUsuarioPorIdAsync(request.UsuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.NoAutorizado, "Usuario no válido o inactivo.");

        var caja = await _almacen.ObtenerCajaPorIdAsync(request.TurnoId);
        if (caja is null || caja.Estado != EstadoCaja.Abierta)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.TurnoCerradoOInexistente, "El turno de caja no existe o está cerrado.");

        var venta = await _almacen.ObtenerVentaPorIdAsync(request.BorradorId);
        if (venta is null)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.NoEncontrado, "La venta o borrador indicado no existe.");

        if (venta.Estado != EstadoVenta.Borrador)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.EstadoInvalido, "La venta ya fue confirmada o anulada.");

        if (venta.Detalles.Count == 0)
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "No se puede confirmar una venta sin productos.");

        // 3. Autorización administrativa para descuentos o venta sin cobro
        RolUsuario rolAutorizador = usuario.Rol;
        if (descuentoPct > 0m || metodoPago == MetodoPago.SinCobroAutorizado)
        {
            var auth = await ValidadorAutorizacionAdmin.ValidarAsync(_almacen, request.Autorizacion);
            if (!auth.Exitoso || auth.Valor is null)
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(auth.Error!.Codigo, auth.Error.Mensaje);

            rolAutorizador = auth.Valor.Rol;
        }

        // 4. Validación previa de stock e importes antes de mutar entidades
        var productosAfectados = new List<(Producto Producto, int Cantidad)>();
        foreach (var detalle in venta.Detalles)
        {
            var producto = await _almacen.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto is null)
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.NoEncontrado, $"El producto #{detalle.ProductoId} no existe.");

            if (!producto.Activo)
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.EstadoInvalido, $"El producto '{producto.Nombre}' está inactivo.");

            if (producto.Stock < detalle.Cantidad)
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.StockInsuficiente, $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}.");

            productosAfectados.Add((producto, detalle.Cantidad));
        }

        decimal bruto = venta.SubtotalBruto;
        decimal descuentoMonetario = decimal.Round(bruto * (descuentoPct / 100m), 2, MidpointRounding.AwayFromZero);
        decimal consumoNeto = decimal.Round(bruto - descuentoMonetario, 2, MidpointRounding.AwayFromZero);
        decimal recibido = decimal.Round(request.Cobro.MontoRecibido, 2, MidpointRounding.AwayFromZero);

        if (metodoPago == MetodoPago.SinCobroAutorizado)
        {
            if (consumoNeto != 0m || propina != 0m || recibido != 0m)
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "SinCobroAutorizado solo se admite cuando el consumo, propina y monto recibido son cero.");
        }
        else if (metodoPago == MetodoPago.Efectivo)
        {
            if (recibido < consumoNeto + propina)
                return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "El efectivo recibido es menor al total a pagar.");
        }
        else if (!request.Cobro.ConfirmacionDigitalManual)
        {
            return ResultadoOperacion<VentaConfirmadaDto>.Fallo(CodigosErrorPos.Validacion, "Debe confirmar manualmente la recepción del cobro digital.");
        }

        string? clienteNombre = null;
        if (venta.ClienteId.HasValue)
        {
            var cliente = await _almacen.ObtenerClientePorIdAsync(venta.ClienteId.Value);
            clienteNombre = cliente?.Nombre;
        }

        // 5. Persistencia atómica en transacción corta
        Pago pago = null!;
        Comprobante comprobante = null!;

        await _almacen.EjecutarEnTransaccionAsync(async () =>
        {
            venta.AplicarDescuento(descuentoPct);
            venta.Confirmar(claveOperacion, caja.Id);

            foreach (var (producto, cantidad) in productosAfectados)
            {
                producto.DescontarStock(cantidad);
                var movStock = MovimientoStock.Crear(
                    producto.Id,
                    request.UsuarioId,
                    TipoMovimientoStock.SalidaProducto,
                    cantidad,
                    $"Venta #{venta.Id}",
                    venta.Id);

                await _almacen.AgregarMovimientoStockAsync(movStock);
            }

            pago = metodoPago switch
            {
                MetodoPago.Efectivo => Pago.RegistrarEfectivo(venta.Id, caja.Id, request.UsuarioId, consumoNeto, propina, recibido),
                MetodoPago.SinCobroAutorizado => Pago.RegistrarSinCobro(venta.Id, caja.Id, request.UsuarioId, rolAutorizador),
                _ => Pago.RegistrarDigital(venta.Id, caja.Id, request.UsuarioId, metodoPago, consumoNeto, propina, request.Cobro.ConfirmacionDigitalManual)
            };

            await _almacen.AgregarPagoAsync(pago);

            if (pago.MontoConsumo > 0m)
            {
                await _almacen.AgregarMovimientoCajaAsync(CajaMovimiento.Crear(
                    caja.Id,
                    request.UsuarioId,
                    TipoMovimientoCaja.Ingreso,
                    OrigenMovimientoCaja.Venta,
                    pago.Metodo,
                    pago.MontoConsumo,
                    $"Cobro venta #{venta.Id}",
                    venta.Id));
            }

            if (pago.MontoPropina > 0m)
            {
                await _almacen.AgregarMovimientoCajaAsync(CajaMovimiento.Crear(
                    caja.Id,
                    request.UsuarioId,
                    TipoMovimientoCaja.Ingreso,
                    OrigenMovimientoCaja.Propina,
                    pago.Metodo,
                    pago.MontoPropina,
                    $"Propina venta #{venta.Id}",
                    venta.Id));
            }

            int siguienteNumero = await _almacen.ObtenerSiguienteNumeroComprobanteAsync(SerieInterna);
            comprobante = Comprobante.Emitir(venta, pago, SerieInterna, siguienteNumero, clienteNombre);
            await _almacen.AgregarComprobanteAsync(comprobante);
        });

        return ResultadoOperacion<VentaConfirmadaDto>.Ok(
            ConstruirDtoConfirmada(venta, pago, comprobante));
    }

    private static BorradorDto MapearBorrador(Venta venta, string? clienteNombre, string? observacionesGenerales)
    {
        var lineas = venta.Detalles
            .Select(d => new LineaBorradorDto(
                d.ProductoId,
                d.NombreProducto,
                d.Cantidad,
                d.PrecioUnitario,
                d.Subtotal,
                d.Observacion))
            .ToList();

        return new BorradorDto(
            venta.Id,
            venta.ClienteId,
            clienteNombre,
            venta.Modalidad == ModalidadVenta.ParaLlevar,
            observacionesGenerales,
            venta.SubtotalBruto,
            lineas,
            venta.Fecha);
    }

    private static VentaConfirmadaDto ConstruirDtoConfirmada(Venta venta, Pago pago, Comprobante comprobante)
    {
        decimal consumoNeto = venta.Total;
        decimal baseInformativa = decimal.Round(consumoNeto / 1.18m, 2, MidpointRounding.AwayFromZero);
        decimal impuestoInformativo = decimal.Round(consumoNeto - baseInformativa, 2, MidpointRounding.AwayFromZero);

        return new VentaConfirmadaDto(
            venta.Id,
            comprobante.NumeroFormateado,
            consumoNeto,
            baseInformativa,
            impuestoInformativo,
            pago.MontoPropina,
            pago.MontoTotal,
            pago.Vuelto);
    }

    private static bool TryParseMetodoPago(string? valor, out MetodoPago metodo)
    {
        metodo = default;
        if (string.IsNullOrWhiteSpace(valor)) return false;

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