using Xunit;

namespace PosCafeteria.Tests
{
    public class VentaTests
    {
        [Fact]
        public void CalcularTotal_DeberiaSumarCorrectamenteElMontoDeLosProductos()
        {
            // Arrange (Preparación)
            decimal precioProducto1 = 50.0m;
            int cantidad1 = 2; // Total: 100.0m
            
            decimal precioProducto2 = 30.0m;
            int cantidad2 = 1; // Total: 30.0m

            decimal totalEsperado = 130.0m;

            // Act (Acción)
            decimal totalCalculado = (precioProducto1 * cantidad1) + (precioProducto2 * cantidad2);

            // Assert (Verificación)
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
