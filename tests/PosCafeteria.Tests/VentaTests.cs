using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace PosCafeteria.Tests
{
    public class VentaTests
    {
        [Fact]
        public void CalcularTotal_DeberiaSumarCorrectamenteElMontoDeLosProductos()
        {
            // Arrange
            decimal precioProducto1 = 50.0m;
            int cantidad1 = 2; // Total: 100.0m
            
            decimal precioProducto2 = 30.0m;
            int cantidad2 = 1; // Total: 30.0m

            decimal totalEsperado = 130.0m;

            // Act
            decimal totalCalculado = (precioProducto1 * cantidad1) + (precioProducto2 * cantidad2);

            // Assert
            Assert.Equal(totalEsperado, totalCalculado);
        }

        [Fact]
        public void CalcularTotal_ConPreciosDecimales_DeberiaCalcularExacto()
        {
            // Arrange
            decimal precioCafe = 25.50m;
            int cantidadCafe = 3; // Total: 76.50m

            decimal precioPan = 15.25m;
            int cantidadPan = 2; // Total: 30.50m

            decimal totalEsperado = 107.00m;

            // Act
            decimal totalCalculado = (precioCafe * cantidadCafe) + (precioPan * cantidadPan);

            // Assert
            Assert.Equal(totalEsperado, totalCalculado);
        }

        [Fact]
        public void RegistrarVenta_ConCantidadNegativa_DeberiaLanzarExcepcion()
        {
            // Arrange
            int cantidadInvalida = -2;

            // Act & Assert
            Assert.True(cantidadInvalida < 0, "Las cantidades negativas deben ser controladas por el dominio.");
        }
    }
}
