// hecho por alex

using System;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Dominio;

public class Venta
{
    // Almacena internamente los detalles de la venta.
    private readonly List<DetalleVenta> _detalles = new();

    public int Id { get; private set; }

    public DateTime Fecha { get; private set; } = DateTime.UtcNow;

    // Clave foránea hacia Clientes.
    // null representa una venta sin cliente registrado.
    public int? ClienteId { get; set; }

    // Propiedad de navegación para acceder al cliente asociado.
    public Cliente? Cliente { get; set; }

    // Permite consultar los detalles sin modificar directamente la lista.
    public IReadOnlyList<DetalleVenta> Detalles => _detalles.AsReadOnly();

    // El total se calcula a partir de las cantidades y precios históricos.
    public decimal Total =>
        _detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

    // Agrega un producto o acumula su cantidad si ya está en la venta.
    public void AgregarDetalle(
        int productoId,
        int cantidad,
        decimal precioUnitario)
    {
        var detalleExistente = _detalles.FirstOrDefault(
            d => d.ProductoId == productoId);

        if (detalleExistente != null)
        {
            detalleExistente.AumentarCantidad(cantidad);
        }
        else
        {
            var detalle = new DetalleVenta(
                productoId,
                cantidad,
                precioUnitario);

            _detalles.Add(detalle);
        }
    }

    // Permite agregar un producto mediante su identificador y precio.
    public void AgregarProducto(
        int productoId,
        int cantidad,
        decimal precioUnitario)
    {
        AgregarDetalle(productoId, cantidad, precioUnitario);
    }

    // Permite agregar un producto utilizando la entidad Producto.
    public void AgregarProducto(Producto producto, int cantidad)
    {
        if (producto == null)
        {
            throw new ArgumentNullException(nameof(producto));
        }

        AgregarDetalle(producto.Id, cantidad, producto.Precio);
    }
}