using Xunit;
using PosCafeteria.Dominio;

namespace PosCafeteria.Tests
{
    public class VentaTests
    {
        [Fact]
        public void RegistrarVenta_ConCantidadNegativa_DeberiaLanzarExcepcion()
        {
            // Prueba original de Brayan (Cantidades inválidas)
            var venta = new Venta();
            var producto = new Producto { Id = 1, Nombre = "Café", Precio = 50.0m, Stock = 10 };

            Assert.Throws<ArgumentException>(() => venta.AgregarProducto(producto, -2));
        }

        [Fact]
        public void CalcularTotal_DeberiaAcumularYConservarPrecioCorrectamente()
        {
            // Prueba original de Brayan (Acumulación y conservación del precio)
            var venta = new Venta();
            var producto = new Producto { Id = 1, Nombre = "Café", Precio = 50.0m, Stock = 10 };

            venta.AgregarProducto(producto, 2);
            venta.AgregarProducto(producto, 1);

            Assert.Equal(150.0m, venta.Total);
        }

        [Fact]
        public void CalcularTotal_ConPreciosDecimales_DeberiaCalcularExacto()
        {
            // Nuestro complemento (Precisión decimal solicitada por QA)
            var venta = new Venta();
            var cafe = new Producto { Id = 1, Nombre = "Café Especial", Precio = 25.50m, Stock = 10 };
            var pan = new Producto { Id = 2, Nombre = "Pan Integral", Precio = 15.25m, Stock = 10 };

            venta.AgregarProducto(cafe, 3); // 76.50
            venta.AgregarProducto(pan, 2);  // 30.50

            decimal totalEsperado = 107.00m;

            Assert.Equal(totalEsperado, venta.Total);
        }
    }
}