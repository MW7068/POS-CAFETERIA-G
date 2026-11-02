using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Venta
{
    private readonly List<DetalleVenta> _detalles = new();

    public int Id { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public bool EstaConfirmada { get; private set; }
    
    public IReadOnlyList<DetalleVenta> Detalles => _detalles.AsReadOnly();

    public decimal Total => _detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

    public void AgregarDetalle(int productoId, int cantidad, decimal precioUnitario)
    {
        if (EstaConfirmada)
            throw new InvalidOperationException("No se pueden agregar productos a una venta ya confirmada.");

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

    public void Confirmar(IDictionary<int, Producto> catalogoProductos)
    {
        if (EstaConfirmada)
            throw new InvalidOperationException("La venta ya se encuentra confirmada.");

        if (!_detalles.Any())
            throw new InvalidOperationException("No se puede confirmar una venta vacía.");

        // 1. Validar existencias acumuladas de TODOS los productos antes de modificar
        foreach (var detalle in _detalles)
        {
            if (!catalogoProductos.TryGetValue(detalle.ProductoId, out var producto))
                throw new InvalidOperationException($"El producto con ID {detalle.ProductoId} no existe en el catálogo.");

            if (!producto.TieneStockSuficiente(detalle.Cantidad))
                throw new InvalidOperationException($"No hay existencias suficientes para '{producto.Nombre}'. Disponible: {producto.Existencias}, requerido: {detalle.Cantidad}.");
        }

        // 2. Descontar stock únicamente si todos los productos cumplen con las existencias
        foreach (var detalle in _detalles)
        {
            catalogoProductos[detalle.ProductoId].DescontarStock(detalle.Cantidad);
        }

        EstaConfirmada = true;
    }
}