using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Venta
{
    private readonly List<DetalleVenta> _detalles = new();

    public int Id { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public EstadoVenta Estado { get; private set; } = EstadoVenta.Borrador;
    public ModalidadAtencion Modalidad { get; set; } = ModalidadAtencion.EnLocal;
    public string? IdentificadorOperacion { get; private set; }
    public int? TurnoCajaId { get; private set; }
    public int? ClienteId { get; private set; }
    public decimal PorcentajeDescuento { get; private set; }

    public IReadOnlyList<DetalleVenta> Detalles => _detalles.AsReadOnly();
    public decimal ConsumoBruto => decimal.Round(_detalles.Sum(d => d.Subtotal), 2, MidpointRounding.AwayFromZero);
    public decimal MontoDescuento => decimal.Round(ConsumoBruto * (PorcentajeDescuento / 100m), 2, MidpointRounding.AwayFromZero);
    public decimal Total => ConsumoBruto - MontoDescuento;
    public decimal MontoImpuesto => Total - decimal.Round(Total / 1.18m, 2, MidpointRounding.AwayFromZero);

    public Venta() { }

    public void AgregarProducto(Producto producto, int cantidad)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        if (!producto.Activo) throw new InvalidOperationException("Producto inactivo.");

        var item = _detalles.FirstOrDefault(d => d.ProductoId == producto.Id);
        if (item != null) item.ActualizarCantidad(item.Cantidad + cantidad);
        else _detalles.Add(new DetalleVenta(producto.Id, producto.Nombre, cantidad, producto.Precio));
    }

    public void QuitarProducto(int productoId)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        var item = _detalles.FirstOrDefault(d => d.ProductoId == productoId);
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
        if (porcentaje < 0m || porcentaje > 100m) throw new ArgumentException("Descuento inválido.");
        PorcentajeDescuento = decimal.Round(porcentaje, 2, MidpointRounding.AwayFromZero);
    }

    public void Confirmar(int turnoCajaId, string? identificadorOperacion = null)
    {
        if (Estado != EstadoVenta.Borrador) throw new InvalidOperationException("Venta cerrada.");
        if (_detalles.Count == 0) throw new InvalidOperationException("Venta vacía.");

        Estado = EstadoVenta.Confirmada;
        TurnoCajaId = turnoCajaId;
        IdentificadorOperacion = string.IsNullOrWhiteSpace(identificadorOperacion) ? Guid.NewGuid().ToString("N") : identificadorOperacion.Trim();
        Fecha = DateTime.UtcNow;
    }

    public void Anular()
    {
        if (Estado != EstadoVenta.Confirmada) throw new InvalidOperationException("Solo se anulan ventas confirmadas.");
        Estado = EstadoVenta.Anulada;
    }
}