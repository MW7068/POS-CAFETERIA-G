using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Venta
{
    private readonly List<DetalleVenta> _detalles = new();

    public int Id { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public DateTime? FechaConfirmacionUtc { get; private set; }

    // PBI-01: Multiventa, modalidad e idempotencia
    public EstadoVenta Estado { get; private set; } = EstadoVenta.Borrador;
    public ModalidadAtencion Modalidad { get; private set; } = ModalidadAtencion.Local;
    public string? IdentificadorOperacion { get; private set; }
    public int? UsuarioCajeroId { get; private set; }
    public int? TurnoCajaId { get; private set; }

    // PBI-06: Cliente opcional e instantánea histórica
    public int? ClienteId { get; private set; }
    public string ClienteNombreSnapshot { get; private set; } = "Público general";
    public TipoDocumentoCliente ClienteDocumentoTipoSnapshot { get; private set; } = TipoDocumentoCliente.Ninguno;
    public string? ClienteDocumentoNumeroSnapshot { get; private set; }

    // PBI-07: Descuentos, impuesto incluido y propina
    public decimal PorcentajeDescuento { get; private set; }
    public string? MotivoDescuento { get; private set; }
    public int? DescuentoAutorizadoPorUsuarioId { get; private set; }
    public decimal TasaImpuestoAplicada { get; private set; } = 18.00m;
    public decimal Propina { get; private set; }

    // PBI-08: Auditoría de anulación total
    public int? AnuladoPorUsuarioId { get; private set; }
    public int? TurnoAnulacionId { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }

    public IReadOnlyList<DetalleVenta> Detalles => _detalles.AsReadOnly();

    // Cálculos monetarios en PEN con redondeo AwayFromZero (PBI-07)
    public decimal ConsumoBruto =>
        decimal.Round(_detalles.Sum(d => d.Cantidad * d.PrecioUnitario), 2, MidpointRounding.AwayFromZero);

    public decimal MontoDescuento =>
        decimal.Round(ConsumoBruto * (PorcentajeDescuento / 100m), 2, MidpointRounding.AwayFromZero);

    public decimal ConsumoNeto => ConsumoBruto - MontoDescuento;

    public decimal BaseImponible => ConsumoNeto == 0m
        ? 0.00m
        : decimal.Round(ConsumoNeto / (1m + (TasaImpuestoAplicada / 100m)), 2, MidpointRounding.AwayFromZero);

    public decimal MontoImpuesto => ConsumoNeto - BaseImponible;

    public decimal TotalAPagar => ConsumoNeto + Propina;

    // Compatibilidad con pruebas existentes (cuando descuento = 0, coincide con la suma de detalles)
    public decimal Total => ConsumoNeto;

    public Venta() { }

    public Venta(int usuarioCajeroId, ModalidadAtencion modalidad = ModalidadAtencion.Local)
    {
        if (usuarioCajeroId <= 0)
            throw new ArgumentException("Se requiere un usuario cajero válido.", nameof(usuarioCajeroId));

        UsuarioCajeroId = usuarioCajeroId;
        Modalidad = modalidad;
        Estado = EstadoVenta.Borrador;
        Fecha = DateTime.UtcNow;
    }

    // --- Gestión de líneas del borrador (PBI-01) ---

    public void AgregarDetalle(int productoId, int cantidad, decimal precioUnitario)
    {
        AgregarDetalleConNombre(productoId, string.Empty, cantidad, precioUnitario);
    }

    public void AgregarProducto(int productoId, int cantidad, decimal precioUnitario)
    {
        AgregarDetalleConNombre(productoId, string.Empty, cantidad, precioUnitario);
    }

    public void AgregarProducto(Producto producto, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(producto);
        if (!producto.Activo)
            throw new InvalidOperationException($"El producto '{producto.Nombre}' está inactivo y no puede agregarse a la venta.");

        AgregarDetalleConNombre(producto.Id, producto.Nombre, cantidad, producto.Precio);
    }

    public void AgregarDetalleConNombre(int productoId, string nombreProducto, int cantidad, decimal precioUnitario)
    {
        AsegurarEsBorrador();

        var detalleExistente = _detalles.FirstOrDefault(d => d.ProductoId == productoId);
        if (detalleExistente != null)
        {
            detalleExistente.AumentarCantidad(cantidad);
        }
        else
        {
            var detalle = new DetalleVenta(productoId, nombreProducto, cantidad, precioUnitario);
            _detalles.Add(detalle);
        }
    }

    public void ActualizarCantidadLinea(int productoId, int nuevaCantidad)
    {
        AsegurarEsBorrador();

        var detalle = _detalles.FirstOrDefault(d => d.ProductoId == productoId)
            ?? throw new InvalidOperationException("El producto no se encuentra en el borrador.");

        detalle.ActualizarCantidad(nuevaCantidad);
    }

    public void EliminarLinea(int productoId)
    {
        AsegurarEsBorrador();

        var detalle = _detalles.FirstOrDefault(d => d.ProductoId == productoId)
            ?? throw new InvalidOperationException("El producto no se encuentra en el borrador.");

        _detalles.Remove(detalle);
    }

    public void RecalcularLineaConCatalogo(Producto producto)
    {
        AsegurarEsBorrador();
        ArgumentNullException.ThrowIfNull(producto);

        var detalle = _detalles.FirstOrDefault(d => d.ProductoId == producto.Id)
            ?? throw new InvalidOperationException("El producto no se encuentra en el borrador.");

        detalle.ActualizarPrecioDesdeCatalogo(producto.Precio, producto.Nombre);
    }

    public void CambiarModalidad(ModalidadAtencion modalidad)
    {
        AsegurarEsBorrador();
        Modalidad = modalidad;
    }

    public void CancelarBorrador()
    {
        AsegurarEsBorrador();
        Estado = EstadoVenta.BorradorCancelado;
    }

    // --- Cliente opcional e instantánea (PBI-06) ---

    public void AsignarPublicoGeneral()
    {
        AsegurarEsBorrador();
        ClienteId = null;
        ClienteNombreSnapshot = "Público general";
        ClienteDocumentoTipoSnapshot = TipoDocumentoCliente.Ninguno;
        ClienteDocumentoNumeroSnapshot = null;
    }

    public void AsignarCliente(int clienteId, string nombre, TipoDocumentoCliente tipoDocumento, string? numeroDocumento)
    {
        AsegurarEsBorrador();
        if (clienteId <= 0)
            throw new ArgumentException("El cliente debe tener un identificador válido.", nameof(clienteId));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(nombre));

        ClienteId = clienteId;
        ClienteNombreSnapshot = nombre.Trim();
        ClienteDocumentoTipoSnapshot = tipoDocumento;
        ClienteDocumentoNumeroSnapshot = string.IsNullOrWhiteSpace(numeroDocumento) ? null : numeroDocumento.Trim();
    }

    // --- Descuentos autorizados (PBI-07) ---

    public void AplicarDescuento(
        decimal porcentaje,
        decimal limitePermitido,
        int? usuarioAutorizadorId,
        RolUsuario? rolAutorizador,
        string? motivo)
    {
        AsegurarEsBorrador();

        if (porcentaje < 0m || porcentaje > 100m)
            throw new ArgumentOutOfRangeException(nameof(porcentaje), "El descuento debe estar entre 0 % y 100 %.");

        decimal porcentajeRedondeado = decimal.Round(porcentaje, 2, MidpointRounding.AwayFromZero);

        if (porcentajeRedondeado > limitePermitido)
            throw new InvalidOperationException($"El descuento ({porcentajeRedondeado}%) supera el límite configurado ({limitePermitido}%).");

        if (porcentajeRedondeado == 0m)
        {
            PorcentajeDescuento = 0m;
            MotivoDescuento = null;
            DescuentoAutorizadoPorUsuarioId = null;
            return;
        }

        if (rolAutorizador != RolUsuario.Administrador || !usuarioAutorizadorId.HasValue || usuarioAutorizadorId.Value <= 0)
            throw new UnauthorizedAccessException("Todo descuento positivo requiere autorización de un administrador.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Todo descuento positivo requiere un motivo obligatorio.", nameof(motivo));

        PorcentajeDescuento = porcentajeRedondeado;
        MotivoDescuento = motivo.Trim();
        DescuentoAutorizadoPorUsuarioId = usuarioAutorizadorId.Value;
    }

    // --- Confirmación de venta (PBI-01 + PBI-07 + PBI-08) ---

    public void Confirmar(
        string identificadorOperacion,
        int turnoCajaId,
        int usuarioCajeroId,
        decimal tasaImpuestoAplicada,
        decimal propina = 0m,
        RolUsuario? rolActor = null)
    {
        AsegurarEsBorrador();

        if (_detalles.Count == 0)
            throw new InvalidOperationException("Debe agregarse al menos un producto antes de confirmar la venta.");

        if (string.IsNullOrWhiteSpace(identificadorOperacion))
            throw new ArgumentException("El identificador único de operación es obligatorio.", nameof(identificadorOperacion));

        if (turnoCajaId <= 0)
            throw new ArgumentException("Se requiere un turno de caja abierto para confirmar la venta.", nameof(turnoCajaId));

        if (usuarioCajeroId <= 0)
            throw new ArgumentException("Se requiere un usuario autenticado para confirmar la venta.", nameof(usuarioCajeroId));

        if (tasaImpuestoAplicada != 18.00m && tasaImpuestoAplicada != 10.50m)
            throw new ArgumentException("La tasa interna aplicada debe ser 18% o 10.5%.", nameof(tasaImpuestoAplicada));

        if (propina < 0m || decimal.Round(propina, 2, MidpointRounding.AwayFromZero) != propina)
            throw new ArgumentException("La propina debe ser un importe no negativo con máximo dos decimales.", nameof(propina));

        if (ConsumoNeto == 0m)
        {
            if (propina > 0m)
                throw new InvalidOperationException("Una venta con consumo neto cero no admite propina.");

            if (rolActor != RolUsuario.Administrador)
                throw new UnauthorizedAccessException("Solo un administrador puede registrar una venta con consumo neto cero.");
        }

        IdentificadorOperacion = identificadorOperacion.Trim();
        TurnoCajaId = turnoCajaId;
        UsuarioCajeroId = usuarioCajeroId;
        TasaImpuestoAplicada = tasaImpuestoAplicada;
        Propina = propina;
        Estado = EstadoVenta.Confirmada;
        FechaConfirmacionUtc = DateTime.UtcNow;
    }

    // --- Anulación total auditable (PBI-08) ---

    public void Anular(int adminUsuarioId, RolUsuario rolAutorizador, int turnoActualId, string motivo)
    {
        if (Estado == EstadoVenta.Anulada)
            throw new InvalidOperationException("La venta ya fue anulada previamente.");

        if (Estado != EstadoVenta.Confirmada)
            throw new InvalidOperationException("Solo pueden anularse ventas confirmadas.");

        if (rolAutorizador != RolUsuario.Administrador)
            throw new UnauthorizedAccessException("Solo un administrador puede autorizar la anulación total de una venta.");

        if (adminUsuarioId <= 0)
            throw new ArgumentException("Se requiere el identificador del administrador que autoriza.", nameof(adminUsuarioId));

        if (turnoActualId <= 0)
            throw new ArgumentException("Se requiere un turno de caja abierto para registrar la anulación.", nameof(turnoActualId));

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        Estado = EstadoVenta.Anulada;
        AnuladoPorUsuarioId = adminUsuarioId;
        TurnoAnulacionId = turnoActualId;
        MotivoAnulacion = motivo.Trim();
        FechaAnulacionUtc = DateTime.UtcNow;
    }

    private void AsegurarEsBorrador()
    {
        if (Estado != EstadoVenta.Borrador)
            throw new InvalidOperationException($"No se puede modificar una venta en estado {Estado}.");
    }
}