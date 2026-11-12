using System;

namespace PosCafeteria.Dominio;

public class DetalleVenta
{
    public int Id { get; private set; }
    public int VentaId { get; set; } // <--- AGREGAR ESTA LÍNEA
    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }

    public DetalleVenta(int productoId, int cantidad, decimal precioUnitario)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

        if (precioUnitario <= 0)
            throw new ArgumentException("El precio debe ser mayor a cero.", nameof(precioUnitario));

        ProductoId = productoId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }

    public void AumentarCantidad(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a agregar debe ser mayor a cero.", nameof(cantidad));

        Cantidad += cantidad;
    }
}
