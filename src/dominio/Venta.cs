using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Venta
{
    private readonly List<DetalleVenta> _detalles = new();

    public int Id { get; private set; }
    public string? IdentificadorOperacion { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public int UsuarioId { get; private set; }
    public EstadoVenta Estado { get; private set; } = EstadoVenta.Borrador;
    public ModalidadVenta Modalidad { get; private set; } = ModalidadVenta.Local;
    public int? NumeroMesa { get; private set; }
    public int? CajaId { get; private set; }
    public int? ClienteId { get; private set; }
    public decimal Descuento { get; private set; }

    public IReadOnlyList<DetalleVenta> Detalles => _detalles.AsReadOnly();

    public decimal SubtotalBruto => decimal.Round(_detalles.Sum(d => d.Subtotal), 2, MidpointRounding.AwayFromZero);

    public decimal Total
    {
        get
        {
            decimal bruto = SubtotalBruto;
            return decimal.Round(bruto - (bruto * (Descuento / 100m)), 2, MidpointRounding.AwayFromZero);
        }
    }

    public Venta() { }

    public Venta(int usuarioId, ModalidadVenta modalidad = ModalidadVenta.Local, int? numeroMesa = null)
    {
        if (usuarioId <= 0) throw new ArgumentException("Usuario inválido.", nameof(usuarioId));
        UsuarioId = usuarioId;
        ConfigurarModalidad(modalidad, numeroMesa);
    }

    public void ConfigurarModalidad(ModalidadVenta modalidad, int? numeroMesa = null)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        Modalidad = modalidad;
        NumeroMesa = modalidad == ModalidadVenta.Local ? numeroMesa : null;
    }

    public void AgregarProducto(Producto producto, int cantidad, string? observacion = null)
    {
        ArgumentNullException.ThrowIfNull(producto);
        if (!producto.Activo) throw new InvalidOperationException("Producto inactivo.");
        AgregarDetalle(producto.Id, cantidad, producto.Precio, producto.Nombre, observacion);
    }

    public void AgregarProducto(int productoId, int cantidad, decimal precioUnitario, string? nombreProducto = null, string? observacion = null)
        => AgregarDetalle(productoId, cantidad, precioUnitario, nombreProducto, observacion);

    public void AgregarDetalle(int productoId, int cantidad, decimal precioUnitario, string? nombreProducto = null, string? observacion = null)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        if (cantidad <= 0) throw new ArgumentException("La cantidad a agregar debe ser mayor que cero.", nameof(cantidad));
        if (precioUnitario < 0m) throw new ArgumentException("El precio unitario no puede ser negativo.", nameof(precioUnitario));

        string? obs = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        var item = _detalles.FirstOrDefault(d => d.ProductoId == productoId && string.Equals(d.Observacion, obs, StringComparison.OrdinalIgnoreCase));

        if (item != null)
            item.ActualizarCantidad(item.Cantidad + cantidad);
        else
            _detalles.Add(new DetalleVenta(productoId, string.IsNullOrWhiteSpace(nombreProducto) ? $"Producto #{productoId}" : nombreProducto, cantidad, precioUnitario, obs));
    }

    public void ActualizarCantidadDetalle(int productoId, int nuevaCantidad, string? observacion = null)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        string? obs = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        var item = _detalles.FirstOrDefault(d => d.ProductoId == productoId && string.Equals(d.Observacion, obs, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Detalle no encontrado en el borrador.");
        item.ActualizarCantidad(nuevaCantidad);
    }

    public void ActualizarObservacionDetalle(int productoId, string? observacionActual, string? nuevaObservacion)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        string? obs = string.IsNullOrWhiteSpace(observacionActual) ? null : observacionActual.Trim();
        var item = _detalles.FirstOrDefault(d => d.ProductoId == productoId && string.Equals(d.Observacion, obs, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Detalle no encontrado en el borrador.");
        item.ActualizarObservacion(nuevaObservacion);
    }

    public void QuitarDetalle(int productoId, string? observacion = null)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        string? obsNormalizada = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        var item = _detalles.FirstOrDefault(d =>
            d.ProductoId == productoId &&
            string.Equals(d.Observacion, obsNormalizada, StringComparison.OrdinalIgnoreCase));

        if (item != null) _detalles.Remove(item);
    }

    public void AsignarCliente(int? clienteId)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        ClienteId = clienteId;
    }

    public void AplicarDescuento(decimal porcentaje)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        if (porcentaje < 0m || porcentaje > 100m) throw new ArgumentException("El descuento debe estar entre 0 y 100.");
        Descuento = decimal.Round(porcentaje, 2, MidpointRounding.AwayFromZero);
    }

    public void Confirmar(string identificadorOperacion, int cajaId)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("La venta ya no está en borrador.");
        if (_detalles.Count == 0) throw new InvalidOperationException("No se puede confirmar una venta vacía.");
        if (string.IsNullOrWhiteSpace(identificadorOperacion))
            throw new ArgumentException("El identificador de operación es obligatorio.", nameof(identificadorOperacion));
        if (cajaId <= 0)
            throw new ArgumentException("Caja inválida.", nameof(cajaId));

        IdentificadorOperacion = identificadorOperacion.Trim();
        CajaId = cajaId;
        Estado = EstadoVenta.Confirmada;
        Fecha = DateTime.UtcNow;
    }

    public void Anular()
    {
        if (Estado != EstadoVenta.Confirmada)
            throw new InvalidOperationException("Solo se pueden anular ventas confirmadas.");
        Estado = EstadoVenta.Anulada;
    }
}