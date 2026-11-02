using Xunit;
using PosCafeteria.Dominio;

namespace PosCafeteria.Tests;

public class VentaTests
{
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
}
