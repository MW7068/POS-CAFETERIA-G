using Xunit;
using PosCafeteria.Dominio;

namespace PosCafeteria.Tests
{
    public class VentaTests
    {
        [Fact]
        public void CalcularTotal_DeberiaSumarCorrectamenteElMontoDeLosProductos()
        {
            // Arrange
            var venta = new Venta();
            var producto1 = new Producto { Id = 1, Nombre = "Café", Precio = 50.0m, Stock = 10 };
            var producto2 = new Producto { Id = 2, Nombre = "Pan", Precio = 30.0m, Stock = 10 };

            // Act
            venta.AgregarProducto(producto1, 2); // 50 * 2 = 100
            venta.AgregarProducto(producto2, 1); // 30 * 1 = 30

            decimal totalEsperado = 130.0m;

            // Assert
            Assert.Equal(totalEsperado, venta.Total);
        }

        [Fact]
        public void CalcularTotal_ConPreciosDecimales_DeberiaCalcularExacto()
        {
            // Arrange
            var venta = new Venta();
            var cafe = new Producto { Id = 1, Nombre = "Café Especial", Precio = 25.50m, Stock = 10 };
            var pan = new Producto { Id = 2, Nombre = "Pan Integral", Precio = 15.25m, Stock = 10 };

            // Act
            venta.AgregarProducto(cafe, 3); // 25.50 * 3 = 76.50
            venta.AgregarProducto(pan, 2);  // 15.25 * 2 = 30.50

            decimal totalEsperado = 107.00m;

            // Assert
            Assert.Equal(totalEsperado, venta.Total);
        }

        [Fact]
        public void RegistrarVenta_ConCantidadNegativa_DeberiaLanzarExcepcion()
        {
            // Arrange
            var venta = new Venta();
            var producto = new Producto { Id = 1, Nombre = "Café", Precio = 50.0m, Stock = 10 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => venta.AgregarProducto(producto, -2));
        }
    }
}