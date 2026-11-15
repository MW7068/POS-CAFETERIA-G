using System;

namespace PosCafeteria.Dominio;

public class DetalleVenta
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public string NombreProducto { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Subtotal => decimal.Round(Cantidad * PrecioUnitario, 2, MidpointRounding.AwayFromZero);

    private DetalleVenta() { }

    public DetalleVenta(int productoId, string nombreProducto, int cantidad, decimal precioUnitario)
    {
        if (productoId <= 0 || cantidad <= 0 || precioUnitario < 0m || string.IsNullOrWhiteSpace(nombreProducto))
            throw new ArgumentException("Detalle de venta inválido.");

        ProductoId = productoId;
        NombreProducto = nombreProducto.Trim();
        Cantidad = cantidad;
        PrecioUnitario = decimal.Round(precioUnitario, 2, MidpointRounding.AwayFromZero);
    }

    public void ActualizarCantidad(int cantidad)
    {
        if (cantidad <= 0) throw new ArgumentException("Cantidad inválida.", nameof(cantidad));
        Cantidad = cantidad;
    }
}