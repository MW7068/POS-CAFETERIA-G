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
// 2. AUTENTICACIÓN Y REAUTENTICACIÓN ADMINISTRATIVA VINCULADA A OPERACIÓN
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
/// Credenciales explícitas para reautenticación administrativa previa a una operación sensible concreta.
/// Vincula la autorización desde su solicitud con el tipo de operación, su referencia específica
/// (ej. BorradorId, VentaId o IdentificadorOperacion) y el importe o porcentaje autorizado,
/// impidiendo que la clave viaje en las solicitudes de negocio o se imprima en ToString().
/// </summary>
public sealed class CredencialesReautenticacion
{
    public string UsuarioAdmin { get; }
    public string ClaveAdmin { get; }
    public string TipoOperacion { get; }
    public string ReferenciaOperacion { get; }
    public decimal? ImporteOValorAutorizado { get; }
    public string Motivo { get; }

    public CredencialesReautenticacion(
        string usuarioAdmin,
        string claveAdmin,
        string tipoOperacion,
        string referenciaOperacion,
        decimal? importeOValorAutorizado,
        string motivo)
    {
        UsuarioAdmin = usuarioAdmin;
        ClaveAdmin = claveAdmin;
        TipoOperacion = tipoOperacion;
        ReferenciaOperacion = referenciaOperacion;
        ImporteOValorAutorizado = importeOValorAutorizado;
        Motivo = motivo;
    }

    public override string ToString() =>
        $"CredencialesReautenticacion(UsuarioAdmin={UsuarioAdmin}, TipoOperacion={TipoOperacion}, Referencia={ReferenciaOperacion}, ClaveAdmin=***)";
}

public sealed record SesionUsuarioDto(
    Guid SesionId,
    int UsuarioId,
    string NombreUsuario,
    string Rol,
    DateTime IniciadaUtc);

/// <summary>
/// Constancia de autorización de un solo uso emitida tras validar CredencialesReautenticacion
/// contra el hash PBKDF2 persistido, verificar usuario activo y confirmar rol Administrador.
/// Reglas de vinculación y no reutilización:
/// 1. Queda ligada exactamente a TipoOperacion, ReferenciaOperacion (ej. BorradorId/VentaId)
///    e ImporteOValorAutorizado (porcentaje de descuento o monto total de la venta).
/// 2. El servicio receptor debe rechazarla (ERR_NO_AUTORIZADO) si no coincide la operación,
///    si cambió el importe/descuento respecto al autorizado, si ya expiró (ExpiraUtc) o si su
///    AutorizacionId ya fue consumida previamente en otra operación.
/// </summary>
public sealed record AutorizacionAdminEmitida(
    Guid AutorizacionId,
    int AdminUsuarioId,
    string AdminNombreUsuario,
    string TipoOperacion,
    string ReferenciaOperacion,
    decimal? ImporteOValorAutorizado,
    string Motivo,
    DateTime EmitidaUtc,
    DateTime ExpiraUtc);

// ============================================================================
// 3. DTOs DE ENTRADA Y SALIDA PARA LOS CONTRATOS MÍNIMOS (Sección 7)
// ============================================================================

// --- Catálogo (PBI-03) ---
public sealed record FiltroCatalogoRequest(string? TextoBusqueda, int? CategoriaId, bool SoloActivos = true);
public sealed record ProductoCatalogoDto(int Id, string Nombre, int CategoriaId, string CategoriaNombre, decimal PrecioFinal, int Stock, bool Activo, string? Icono);

// --- Ventas y Borradores (PBI-01 / PBI-07) ---
public sealed record LineaBorradorRequest(int ProductoId, int Cantidad, string? Observaciones = null);

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
/// SinCobroAutorizado requieren AutorizacionAdminEmitida vinculada al borrador e importe/descuento.
/// </summary>
public sealed record DatosCobroRequest(
    string MetodoPago,
    decimal DescuentoPorcentaje,
    decimal Propina,
    decimal MontoRecibido,
    bool ConfirmacionDigitalManual);

/// <summary>
/// Solicitud de confirmación de venta. Requiere AutorizacionAdminEmitida de un solo uso
/// vinculada a BorradorId y al descuento/importe siempre que Cobro.DescuentoPorcentaje &gt; 0
/// o cuando el método sea SinCobroAutorizado.
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
/// Solicitud de movimiento manual de inventario. El servicio de backend debe validar que
/// SesionLocalId corresponda a una sesión administrativa local activa y vigente (usuario activo
/// con rol Administrador); si no existe una sesión administrativa válida, la operación se rechaza
/// explícitamente con ERR_NO_AUTORIZADO o ERR_PERMISO_INSUFICIENTE.
/// </summary>
public sealed record MovimientoStockRequest(
    int ProductoId,
    string TipoMovimiento,
    int Cantidad,
    string Motivo,
    int AdminUsuarioId,
    Guid SesionLocalId);

// --- Clientes (PBI-06) ---
public sealed record GestionarClienteRequest(int? ClienteId, string TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono, string? Direccion, string? Email);
public sealed record ClienteDto(int Id, string TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono, string? Direccion, string? Email);

// --- Caja y Anulaciones (PBI-08) ---
public sealed record AbrirTurnoRequest(int UsuarioId, decimal MontoApertura);
public sealed record RegistrarMovimientoCajaRequest(int TurnoId, int UsuarioId, string TipoMovimiento, decimal Monto, string Motivo);
public sealed record CerrarTurnoRequest(int TurnoId, int UsuarioId, decimal EfectivoContado);
public sealed record ResumenCierreDto(int TurnoId, decimal EfectivoEsperado, decimal EfectivoContado, decimal Diferencia, decimal TotalDigitalFueraGaveta, DateTime FechaCierreUtc);

/// <summary>
/// Solicitud de anulación de venta. Requiere AutorizacionAdminEmitida de un solo uso
/// vinculada específicamente a VentaId y al importe de la venta anulada.
/// </summary>
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

    /// <summary>
    /// Valida las credenciales del administrador y emite una constancia de un solo uso
    /// vinculada a TipoOperacion, ReferenciaOperacion e ImporteOValorAutorizado.
    /// </summary>
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
    /// Registra una entrada o salida manual de stock. Rechaza explícitamente la operación
    /// (ERR_NO_AUTORIZADO / ERR_PERMISO_INSUFICIENTE) si no existe una sesión administrativa
    /// local activa y vigente asociada a SesionLocalId.
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