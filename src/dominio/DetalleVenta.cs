using System;

namespace PosCafeteria.Dominio;

public class DetalleVenta
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    // Constructor restringido para EF Core
    internal DetalleVenta() { }

    public DetalleVenta(int productoId, int cantidad, decimal precioUnitario)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

        if (precioUnitario <= 0)
            throw new ArgumentException("El precio unitario debe ser mayor a cero.", nameof(precioUnitario));

        ProductoId = productoId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }

    public DetalleVenta(Producto producto, int cantidad)
        : this(producto?.Id ?? throw new ArgumentNullException(nameof(producto)), cantidad, producto?.Precio ?? throw new ArgumentNullException(nameof(producto)))
    {
    }

    internal void AumentarCantidad(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a agregar debe ser mayor a cero.", nameof(cantidad));

        Cantidad += cantidad;
    }
}
