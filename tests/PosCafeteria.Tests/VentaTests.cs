using Xunit;
using PosCafeteria.Dominio;

namespace PosCafeteria.Tests;

public class VentaTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void AgregarDetalle_CantidadInvalida_LanzaExcepcion(int cantidadInvalida)
    {
        var venta = new Venta();

        Assert.Throws<ArgumentException>(() => 
            venta.AgregarDetalle(productoId: 1, cantidad: cantidadInvalida, precioUnitario: 10.0m));
    }
    [Fact]
    public void RegistrarVenta_DebeCalcularTotalCorrectamente()
    {
        // Arrange
        var venta = new Venta();
        var cafe = new Producto { Id = 1, Nombre = "Café", Precio = 5.00m, Stock = 10 };
        var sandwich = new Producto { Id = 2, Nombre = "Sándwich", Precio = 8.00m, Stock = 10 };

        // Act
        venta.AgregarProducto(cafe, 2);
        venta.AgregarProducto(sandwich, 1);

        // Assert
        Assert.Equal(18.00m, venta.Total);
    }
    [Fact]
    public void AgregarProducto_CantidadCeroOMenos_DebeLanzarExcepcion()
    {
        // Arrange
        var venta = new Venta();
        var cafe = new Producto { Id = 1, Nombre = "Café", Precio = 5.00m, Stock = 10 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => venta.AgregarProducto(cafe, 0));
    }
    [Fact]
    public void AgregarProducto_ProductoExistente_DebeAcumularCantidadYRecalcularTotal()
    {
        // Arrange
        var venta = new Venta();
        var cafe = new Producto { Id = 1, Nombre = "Café", Precio = 5.00m, Stock = 10 };

        // Act
        venta.AgregarProducto(cafe, 2);
        venta.AgregarProducto(cafe, 3); // Mismo producto cargado por segunda vez

        // Assert
        Assert.Single(venta.Detalles);
        Assert.Equal(5, venta.Detalles[0].Cantidad);
        Assert.Equal(25.00m, venta.Total);
    }
    [Fact]
    public void AgregarProducto_CambioPrecioPosteriorEnProducto_NoDebeAfectarPrecioVenta()
    {
        // Arrange
        var venta = new Venta();
        var cafe = new Producto { Id = 1, Nombre = "Café", Precio = 5.00m, Stock = 10 };

        // Act
        venta.AgregarProducto(cafe, 1);
        cafe.Precio = 10.00m; // Se actualiza el precio en el catálogo

        // Assert
        Assert.Equal(5.00m, venta.Total); // El detalle de la venta debe mantener los 5.00m iniciales
    }
}
