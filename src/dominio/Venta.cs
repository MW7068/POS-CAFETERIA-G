namespace PosCafeteria.Dominio;

public class Venta
{
    public List<DetalleVenta> Detalles { get; private set; } = new();
    public decimal Total => Detalles.Sum(d => d.Subtotal);

    public void AgregarProducto(Producto producto, int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.");

        var detalleExistente = Detalles.FirstOrDefault(d => d.ProductoId == producto.Id);
        if (detalleExistente != null)
        {
            detalleExistente.Cantidad += cantidad;
        }
        else
        {
            Detalles.Add(new DetalleVenta
            {
                ProductoId = producto.Id,
                NombreProducto = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = cantidad
            });
        }
    }
}
