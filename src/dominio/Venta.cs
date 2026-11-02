using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Venta
{
    private readonly List<DetalleVenta> _detalles = new();

    public int Id { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    
    public IReadOnlyList<DetalleVenta> Detalles => _detalles.AsReadOnly();

    public decimal Total => _detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

    public void AgregarDetalle(int productoId, int cantidad, decimal precioUnitario)
    {
        var detalleExistente = _detalles.FirstOrDefault(d => d.ProductoId == productoId);
        if (detalleExistente != null)
        {
            detalleExistente.AumentarCantidad(cantidad);
        }
        else
        {
            var detalle = new DetalleVenta(productoId, cantidad, precioUnitario);
            _detalles.Add(detalle);
        }
    }

    public void AgregarProducto(int productoId, int cantidad, decimal precioUnitario)
    {
        AgregarDetalle(productoId, cantidad, precioUnitario);
    }

    public void AgregarProducto(Producto producto, int cantidad)
    {
        if (producto == null) throw new ArgumentNullException(nameof(producto));
        AgregarDetalle(producto.Id, cantidad, producto.Precio);
    }
}