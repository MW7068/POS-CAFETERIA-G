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
// 2. AUTENTICACIÓN Y REAUTENTICACIÓN ADMINISTRATIVA SEPARADA DE NEGOCIO
// ============================================================================
/// <summary>
/// Credenciales de inicio de sesión local. Se modela como clase (no record) y enmascara
/// la contraseña en ToString() para evitar su exposición en trazas o registros.
/// </summary>
public sealed class CredencialesIngreso
{
    public string NombreUsuario { get; }
    public string Clave { get; }

    public CredencialesIngreso(string nombreUsuario, string clave)
    {
        NombreUsuario = nombreUsuario;
        Clave = clave;
    }

    public override string ToString() => $"CredencialesIngreso(Usuario={NombreUsuario}, Clave=***)";
}

/// <summary>
/// Credenciales explícitas para reautenticación administrativa previa a operaciones sensibles.
/// Se modela como clase (no record) para que ClaveAdmin nunca viaje ni se imprima dentro de
/// las solicitudes de negocio.
/// </summary>
public sealed class CredencialesReautenticacion
{
    public string UsuarioAdmin { get; }
    public string ClaveAdmin { get; }
    public string Motivo { get; }

    public CredencialesReautenticacion(string usuarioAdmin, string claveAdmin, string motivo)
    {
        UsuarioAdmin = usuarioAdmin;
        ClaveAdmin = claveAdmin;
        Motivo = motivo;
    }

    public override string ToString() => $"CredencialesReautenticacion(UsuarioAdmin={UsuarioAdmin}, ClaveAdmin=***)";
}

public sealed record SesionUsuarioDto(
    Guid SesionId,
    int UsuarioId,
    string NombreUsuario,
    string Rol,
    DateTime IniciadaUtc);

/// <summary>
/// Constancia emitida tras validar CredencialesReautenticacion contra el hash PBKDF2 persistido,
/// verificar usuario activo y confirmar rol Administrador. No contiene contraseñas.
/// </summary>
public sealed record AutorizacionAdminEmitida(
    Guid AutorizacionId,
    int AdminUsuarioId,
    string AdminNombreUsuario,
    string Motivo,
    DateTime ExpiraUtc);

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

/// <summary>
/// Datos del cobro. Todo descuento positivo (DescuentoPorcentaje &gt; 0) y toda operación
/// SinCobroAutorizado requieren AutorizacionAdminEmitida emitida por un Administrador activo.
/// </summary>
public sealed record DatosCobroRequest(
    string MetodoPago,
    decimal DescuentoPorcentaje,
    decimal Propina,
    decimal MontoRecibido,
    bool ConfirmacionDigitalManual);

/// <summary>
/// Solicitud de confirmación de venta. Requiere AutorizacionAdminEmitida siempre que
/// Cobro.DescuentoPorcentaje sea mayor que cero (&gt; 0) o cuando el método sea SinCobroAutorizado.
/// </summary>
public sealed record ConfirmarVentaRequest(
    Guid IdentificadorOperacion,
    int BorradorId,
    int UsuarioId,
    int TurnoId,
    DatosCobroRequest Cobro,
    AutorizacionAdminEmitida? Autorizacion);

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
/// <summary>
/// Solicitud de movimiento manual de inventario. El servicio de backend verifica la sesión local
/// vigente del usuario autenticado (activo y con rol Administrador), no basta con enviar AdminUsuarioId.
/// </summary>
public sealed record MovimientoStockRequest(
    int ProductoId,
    string TipoMovimiento,
    int Cantidad,
    string Motivo,
    int AdminUsuarioId,
    Guid? SesionLocalId = null);

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
    AutorizacionAdminEmitida Autorizacion,
    bool ReembolsoExternoVerificado);

// --- Comprobantes (PBI-05) ---
public sealed record ComprobanteDto(int ComprobanteId, string Serie, int Numero, string RotuloLegal, DateTime FechaEmisionUtc, string Estado, decimal Total, string ContenidoResumen);

// --- Reportes (PBI-04) ---
/// <summary>
/// Cobertura Issue #4: cubre totales financieros agregados (bruto, anulaciones/reembolsos, neto, propinas),
/// desglose por método de pago y ranking de productos más vendidos en el rango local convertido a UTC.
/// Pendiente Issue #4: listado/detalle de filas de ventas, historial de cierres de caja,
/// exportación a archivo (CSV/PDF) y comparativa interanual/mensual.
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
// 4. INTERFACES DE CONTRATO (16 métodos en 9 interfaces que cubren los contratos mínimos)
// ============================================================================
public interface IAutenticacionServicio
{
    Task<ResultadoOperacion<SesionUsuarioDto>> IniciarSesionAsync(CredencialesIngreso credenciales);
    Task<ResultadoOperacion<AutorizacionAdminEmitida>> ReautenticarAdminAsync(CredencialesReautenticacion credenciales);
}

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
    /// <summary>
    /// Registra una entrada o salida manual de stock validando la sesión local activa
    /// y el rol Administrador del usuario autenticado (no basta el ID recibido).
    /// </summary>
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
    /// <summary>
    /// Consulta indicadores agregados del negocio. Las filas detalladas de ventas y el
    /// historial de cierres de caja quedan explícitamente pendientes para la siguiente entrega de Issue #4.
    /// </summary>
    Task<ResultadoOperacion<ReporteNegocioDto>> ConsultarReporteAsync(FiltroReporteRequest filtro);
}

public interface IRespaldoServicio
{
    Task<ResultadoOperacion<ResultadoRespaldoDto>> CrearRespaldoAsync(string rutaDestino, AutorizacionAdminEmitida autorizacion);
    Task<ResultadoOperacion> RestaurarRespaldoAsync(string rutaArchivoCandidato, AutorizacionAdminEmitida autorizacion);
}