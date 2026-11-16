using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosCafeteria.Dominio.Contratos;

// ============================================================================
// 1. CÓDIGOS DE ERROR COMPARTIDOS Y RESULTADO ESTÁNDAR (Sección 5 y 7)
// ============================================================================
public static class CodigosErrorPos
{
    public const string Validacion = "ERR_VALIDACION";
    public const string NoEncontrado = "ERR_NO_ENCONTRADO";
    public const string NoAutorizado = "ERR_NO_AUTORIZADO";
    public const string PermisoInsuficiente = "ERR_PERMISO_INSUFICIENTE";
    public const string StockInsuficiente = "ERR_STOCK_INSUFICIENTE";
    public const string TurnoCerradoOInexistente = "ERR_TURNO_INVALIDO";
    public const string TurnoYaAbierto = "ERR_TURNO_YA_ABIERTO";
    public const string EstadoInvalido = "ERR_ESTADO_INVALIDO";
    public const string OperacionDuplicada = "ERR_OPERACION_DUPLICADA";
    public const string ConflictoRespaldo = "ERR_CONFLICTO_RESPALDO";
}

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

// ============================================================================
// 2. AUTORIZACIÓN ADMINISTRATIVA VERIFICABLE (No basta enviar AdminUsuarioId)
// ============================================================================
/// <summary>
/// Credenciales explícitas requeridas para operaciones sensibles (descuentos fuera de límite,
/// venta sin cobro, anulaciones o respaldos). El servicio de backend debe autenticar
/// UsuarioAdmin + ClaveAdmin contra el hash PBKDF2 persistido, verificar que esté activo
/// y confirmar que posea rol Administrador; recibir solo un ID no autoriza la operación.
/// </summary>
public sealed record AutorizacionAdminRequest(string UsuarioAdmin, string ClaveAdmin, string Motivo);

// ============================================================================
// 3. DTOs DE ENTRADA Y SALIDA PARA LOS CONTRATOS MÍNIMOS (Sección 7)
// ============================================================================

// --- Catálogo (PBI-03) ---
public sealed record FiltroCatalogoRequest(string? TextoBusqueda, int? CategoriaId, bool SoloActivos = true);
public sealed record ProductoCatalogoDto(int Id, string Nombre, int CategoriaId, string CategoriaNombre, decimal PrecioFinal, int Stock, bool Activo, string? Icono);

// --- Ventas y Borradores (PBI-01 / PBI-07) ---
// Entrada al guardar borrador: solo recibe producto, cantidad y observaciones.
public sealed record LineaBorradorRequest(int ProductoId, int Cantidad, string? Observaciones = null);

// Salida de borrador: incluye snapshot de nombre, precio aplicado, importe y observaciones
// para recuperar la venta sin recalcularla desde el catálogo.
public sealed record LineaBorradorDto(
    int ProductoId,
    string NombreProducto,
    int Cantidad,
    decimal PrecioAplicado,
    decimal Importe,
    string? Observaciones);

public sealed record GuardarBorradorRequest(
    int? BorradorId,
    int UsuarioId,
    int? ClienteId,
    bool EsParaLlevar,
    string? ObservacionesGenerales,
    IReadOnlyList<LineaBorradorRequest> Lineas);

public sealed record BorradorDto(
    int Id,
    int? ClienteId,
    string? ClienteNombre,
    bool EsParaLlevar,
    string? ObservacionesGenerales,
    decimal ConsumoBruto,
    IReadOnlyList<LineaBorradorDto> Lineas,
    DateTime FechaActualizacionUtc);

public sealed record DatosCobroRequest(
    string MetodoPago,
    decimal DescuentoPorcentaje,
    decimal Propina,
    decimal MontoRecibido,
    bool ConfirmacionDigitalManual);

public sealed record ConfirmarVentaRequest(
    Guid IdentificadorOperacion,
    int BorradorId,
    int UsuarioId,
    int TurnoId,
    DatosCobroRequest Cobro,
    AutorizacionAdminRequest? Autorizacion);

public sealed record VentaConfirmadaDto(
    int VentaId,
    string SerieNumeroComprobante,
    decimal ConsumoNeto,
    decimal BaseInformativa,
    decimal ImpuestoInformativo,
    decimal Propina,
    decimal TotalPagado,
    decimal Vuelto);

// --- Inventario (PBI-02) ---
public sealed record MovimientoStockRequest(int ProductoId, string TipoMovimiento, int Cantidad, string Motivo, int AdminUsuarioId);

// --- Clientes (PBI-06) ---
public sealed record GestionarClienteRequest(int? ClienteId, string TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono, string? Direccion, string? Email);
public sealed record ClienteDto(int Id, string TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono, string? Direccion, string? Email);

// --- Caja y Anulaciones (PBI-08) ---
public sealed record AbrirTurnoRequest(int UsuarioId, decimal MontoApertura);
public sealed record RegistrarMovimientoCajaRequest(int TurnoId, int UsuarioId, string TipoMovimiento, decimal Monto, string Motivo);
public sealed record CerrarTurnoRequest(int TurnoId, int UsuarioId, decimal EfectivoContado);
public sealed record ResumenCierreDto(int TurnoId, decimal EfectivoEsperado, decimal EfectivoContado, decimal Diferencia, decimal TotalDigitalFueraGaveta, DateTime FechaCierreUtc);

public sealed record AnularVentaRequest(
    Guid IdentificadorOperacion,
    int VentaId,
    int TurnoActualId,
    AutorizacionAdminRequest Autorizacion,
    bool ReembolsoExternoVerificado);

// --- Comprobantes (PBI-05) ---
public sealed record ComprobanteDto(int ComprobanteId, string Serie, int Numero, string RotuloLegal, DateTime FechaEmisionUtc, string Estado, decimal Total, string ContenidoResumen);

// --- Reportes (PBI-04) ---
/// <summary>
/// Cobertura Issue #4: cubre totales financieros (bruto, anulaciones/reembolsos, neto, propinas),
/// desglose por método de pago y ranking de productos más vendidos en el rango local convertido a UTC.
/// Pendiente Issue #4: exportación a archivo (CSV/PDF) y comparativa interanual/mensual.
/// </summary>
public sealed record FiltroReporteRequest(DateTime FechaInicioLocal, DateTime FechaFinLocal, int? TurnoId, int? ClienteId, string? MetodoPago);
public sealed record ResumenMetodoPagoDto(string MetodoPago, int CantidadOperaciones, decimal TotalRecaudado);
public sealed record ProductoMasVendidoDto(int ProductoId, string NombreProducto, int UnidadesVendidas, decimal ImporteTotal);
public sealed record ReporteNegocioDto(
    decimal BrutoConfirmado,
    decimal Reembolsos,
    decimal Neto,
    decimal Propinas,
    int CantidadVentas,
    IReadOnlyList<ResumenMetodoPagoDto> PorMetodoPago,
    IReadOnlyList<ProductoMasVendidoDto> ProductosMasVendidos);

// --- Respaldo y Restauración (PBI-10) ---
public sealed record ResultadoRespaldoDto(string RutaArchivo, string VersionEsquema, DateTime FechaUtc);

// ============================================================================
// 4. INTERFACES DE CONTRATO (14 métodos en 8 interfaces que cubren los 13 contratos mínimos)
// ============================================================================
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
    Task<ResultadoOperacion<ResultadoRespaldoDto>> CrearRespaldoAsync(string rutaDestino, AutorizacionAdminRequest autorizacion);
    Task<ResultadoOperacion> RestaurarRespaldoAsync(string rutaArchivoCandidato, AutorizacionAdminRequest autorizacion);
}