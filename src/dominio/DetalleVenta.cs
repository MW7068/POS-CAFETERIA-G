using System;

namespace PosCafeteria.Dominio;

public class DetalleVenta
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public string NombreProductoSnapshot { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }

    public decimal Subtotal => decimal.Round(Cantidad * PrecioUnitario, 2, MidpointRounding.AwayFromZero);

    // Constructor privado para EF Core
    private DetalleVenta() { }

    // Constructor de compatibilidad con pruebas existentes
    public DetalleVenta(int productoId, int cantidad, decimal precioUnitario)
        : this(productoId, string.Empty, cantidad, precioUnitario)
    {
    }

    public DetalleVenta(int productoId, string nombreProductoSnapshot, int cantidad, decimal precioUnitario)
    {
        if (productoId <= 0)
            throw new ArgumentException("El identificador del producto debe ser válido.", nameof(productoId));

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

        Producto.ValidarPrecio(precioUnitario);

        ProductoId = productoId;
        NombreProductoSnapshot = nombreProductoSnapshot?.Trim() ?? string.Empty;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }

    public void AumentarCantidad(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a agregar debe ser mayor a cero.", nameof(cantidad));

        long nuevaCantidad = (long)Cantidad + cantidad;
        if (nuevaCantidad > int.MaxValue)
            throw new OverflowException("La cantidad supera el límite permitido.");

        Cantidad = (int)nuevaCantidad;
    }

    public void ActualizarCantidad(int nuevaCantidad)
    {
        if (nuevaCantidad <= 0)
            throw new ArgumentException("La cantidad debe ser un entero positivo.", nameof(nuevaCantidad));

        Cantidad = nuevaCantidad;
    }

    internal void ActualizarPrecioDesdeCatalogo(decimal nuevoPrecio, string? nuevoNombre = null)
    {
        Producto.ValidarPrecio(nuevoPrecio);
        PrecioUnitario = nuevoPrecio;
        if (!string.IsNullOrWhiteSpace(nuevoNombre))
            NombreProductoSnapshot = nuevoNombre.Trim();
    }
}