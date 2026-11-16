using System;
using System.Collections.Generic;

namespace PosCafeteria.Dominio.Contratos;

// 1. Estructura estándar de salida y errores concretos (Sección 5)
public sealed record ErrorContrato(string Codigo, string Mensaje);

public sealed record ResultadoOperacion(bool Exitoso, ErrorContrato? Error = null)
{
    public static ResultadoOperacion Ok() => new(true);
    public static ResultadoOperacion Fallo(string codigo, string mensaje) => new(false, new(codigo, mensaje));
}

public sealed record ResultadoOperacion<T>(bool Exitoso, T? Valor = default, ErrorContrato? Error = null)
{
    public static ResultadoOperacion<T> Ok(T valor) => new(true, valor);
    public static ResultadoOperacion<T> Fallo(string codigo, string mensaje) => new(false, default, new(codigo, mensaje));
}

// 2. DTOs de entrada y salida para las 13 operaciones mínimas (Sección 7)
public sealed record FiltroCatalogoRequest(string? TextoBusqueda, int? CategoriaId, bool SoloActivos = true);
public sealed record ProductoCatalogoDto(int Id, string Nombre, int CategoriaId, string CategoriaNombre, decimal PrecioFinal, int Stock, bool Activo, string? Icono);

public sealed record LineaBorradorRequest(int ProductoId, int Cantidad);
public sealed record GuardarBorradorRequest(int? BorradorId, int UsuarioId, int? ClienteId, bool EsParaLlevar, IReadOnlyList<LineaBorradorRequest> Lineas);
public sealed record BorradorDto(int Id, int? ClienteId, string? ClienteNombre, bool EsParaLlevar, decimal ConsumoBruto, IReadOnlyList<LineaBorradorRequest> Lineas, DateTime FechaActualizacionUtc);

public sealed record DatosCobroRequest(string MetodoPago, decimal DescuentoPorcentaje, decimal Propina, decimal MontoRecibido, bool ConfirmacionDigitalManual);
public sealed record AutorizacionAdminRequest(int AdminUsuarioId, string Motivo);
public sealed record ConfirmarVentaRequest(
    Guid IdentificadorOperacion,
    int BorradorId,
    int UsuarioId,
    int TurnoId,
    DatosCobroRequest Cobro,
    AutorizacionAdminRequest? Autorizacion);
public sealed record VentaConfirmadaDto(int VentaId, string SerieNumeroComprobante, decimal ConsumoNeto, decimal BaseInformativa, decimal ImpuestoInformativo, decimal Propina, decimal TotalPagado, decimal Vuelto);

public sealed record MovimientoStockRequest(int ProductoId, string TipoMovimiento, int Cantidad, string Motivo, int AdminUsuarioId);

public sealed record GestionarClienteRequest(int? ClienteId, string TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono, string? Direccion, string? Email);
public sealed record ClienteDto(int Id, string TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono, string? Direccion, string? Email);

public sealed record AbrirTurnoRequest(int UsuarioId, decimal MontoApertura);
public sealed record RegistrarMovimientoCajaRequest(int TurnoId, int UsuarioId, string TipoMovimiento, decimal Monto, string Motivo);
public sealed record CerrarTurnoRequest(int TurnoId, int UsuarioId, decimal EfectivoContado);
public sealed record ResumenCierreDto(int TurnoId, decimal EfectivoEsperado, decimal EfectivoContado, decimal Diferencia, decimal TotalDigitalFueraGaveta, DateTime FechaCierreUtc);

public sealed record AnularVentaRequest(Guid IdentificadorOperacion, int VentaId, int TurnoActualId, int AdminUsuarioId, string Motivo, bool ReembolsoExternoVerificado);

public sealed record ComprobanteDto(int ComprobanteId, string Serie, int Numero, string RotuloLegal, DateTime FechaEmisionUtc, string Estado, decimal Total, string ContenidoResumen);

public sealed record FiltroReporteRequest(DateTime FechaInicioLocal, DateTime FechaFinLocal, int? TurnoId, int? ClienteId, string? MetodoPago);
public sealed record ReporteNegocioDto(decimal BrutoConfirmado, decimal Reembolsos, decimal Neto, decimal Propinas, int CantidadVentas);

public sealed record ResultadoRespaldoDto(string RutaArchivo, string VersionEsquema, DateTime FechaUtc);

// 3. Interfaces con los 13 contratos mínimos de la Sección 7
public interface ICatalogoServicio
{
    Task<ResultadoOperacion<IReadOnlyList<ProductoCatalogoDto>>> ConsultarCatalogoAsync(FiltroCatalogoRequest filtro);
}

public interface IVentaServicio
{
    Task<ResultadoOperacion<BorradorDto>> GuardarBorradorAsync(GuardarBorradorRequest request);
    Task<ResultadoOperacion<IReadOnlyList<BorradorDto>>> ObtenerBorradoresAsync(int usuarioId);
    Task<ResultadoOperacion<VentaConfirmadaDto>> ConfirmarVentaAsync(ConfirmarVentaRequest request);
}

public interface IInventarioServicio
{
    Task<ResultadoOperacion> RegistrarMovimientoStockAsync(MovimientoStockRequest request);
}

public interface IClienteServicio
{
    Task<ResultadoOperacion<ClienteDto>> GestionarClienteAsync(GestionarClienteRequest request);
}

public interface ICajaServicio
{
    Task<ResultadoOperacion<int>> AbrirTurnoAsync(AbrirTurnoRequest request);
    Task<ResultadoOperacion> RegistrarMovimientoCajaAsync(RegistrarMovimientoCajaRequest request);
    Task<ResultadoOperacion<ResumenCierreDto>> CerrarTurnoAsync(CerrarTurnoRequest request);
    Task<ResultadoOperacion> AnularVentaAsync(AnularVentaRequest request);
}

public interface IComprobanteServicio
{
    Task<ResultadoOperacion<ComprobanteDto>> ObtenerComprobanteAsync(int ventaId);
}

public interface IReporteServicio
{
    Task<ResultadoOperacion<ReporteNegocioDto>> ConsultarReporteAsync(FiltroReporteRequest filtro);
}

public interface IRespaldoServicio
{
    Task<ResultadoOperacion<ResultadoRespaldoDto>> CrearRespaldoAsync(string rutaDestino, int adminUsuarioId);
    Task<ResultadoOperacion> RestaurarRespaldoAsync(string rutaArchivoCandidato, int adminUsuarioId);
}