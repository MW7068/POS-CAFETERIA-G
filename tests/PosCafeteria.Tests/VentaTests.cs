using System;
using System.Collections.Generic;
using PosCafeteria.Dominio;
using Xunit;

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
        venta.AgregarProducto(cafe, 3);

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
        cafe.Precio = 10.00m;

        // Assert
        Assert.Equal(5.00m, venta.Total);
    }

    // --- Nuevas pruebas solicitadas en revisión del PR #23 ---

    [Fact]
    public void EmitirComprobante_PagoYVentaConIdentificadoresEnCero_LanzaExcepcion()
    {
        var venta = new Venta(usuarioId: 1); // venta.Id == 0
        venta.AgregarDetalle(productoId: 1, cantidad: 1, precioUnitario: 10.00m, nombreProducto: "Café");
        venta.Confirmar("OP-000", cajaId: 1);

        var pagoConIdCero = Pago.RegistrarEfectivo(ventaId: 0, cajaId: 1, usuarioId: 1, consumo: 10.00m, propina: 0m, recibido: 10.00m);

        Assert.Throws<InvalidOperationException>(() =>
            Comprobante.Emitir(venta, pagoConIdCero, "INT-001", 1));
    }
    private static void AsignarIdVenta(Venta venta, int id)
    {
    typeof(Venta).GetProperty(nameof(Venta.Id))!.SetValue(venta, id);
    }

    [Fact]
    public void EmitirComprobante_SinCobroAutorizadoConVentaTotalMayorACero_LanzaExcepcion()
    {
        var venta = new Venta(usuarioId: 1);
        AsignarIdVenta(venta, 10);
        venta.AgregarDetalle(productoId: 1, cantidad: 1, precioUnitario: 12.00m, nombreProducto: "Latte");
        venta.Confirmar("OP-010", cajaId: 1);

        var pagoSinCobro = Pago.RegistrarSinCobro(ventaId: venta.Id, cajaId: 1, usuarioId: 1, RolUsuario.Administrador);

        Assert.Throws<InvalidOperationException>(() =>
            Comprobante.Emitir(venta, pagoSinCobro, "INT-001", 10));
    }

    [Fact]
    public void EmitirComprobante_SinCobroAutorizadoConVentaTotalCero_EmiteCorrectamente()
    {
        var venta = new Venta(usuarioId: 1);
        AsignarIdVenta(venta, 11);
        venta.AgregarDetalle(productoId: 1, cantidad: 1, precioUnitario: 10.00m, nombreProducto: "Cortesía");
        venta.AplicarDescuento(100m);
        venta.Confirmar("OP-011", cajaId: 1);

        var pagoSinCobro = Pago.RegistrarSinCobro(ventaId: venta.Id, cajaId: 1, usuarioId: 1, RolUsuario.Administrador);
        var comprobante = Comprobante.Emitir(venta, pagoSinCobro, "INT-001", 11);

        Assert.Equal(venta.Id, comprobante.VentaId);
        Assert.Equal(0m, comprobante.Total);
    }

    [Fact]
    public void EmitirComprobante_PagoDeOtraVenta_LanzaExcepcion()
    {
        var venta = new Venta(usuarioId: 1);
        AsignarIdVenta(venta, 1);
        venta.AgregarDetalle(productoId: 1, cantidad: 1, precioUnitario: 10.00m, nombreProducto: "Café");
        venta.Confirmar("OP-001", cajaId: 1);

        var pagoOtraVenta = Pago.RegistrarEfectivo(ventaId: 999, cajaId: 1, usuarioId: 1, consumo: 10.00m, propina: 0m, recibido: 10.00m);

        Assert.Throws<InvalidOperationException>(() =>
            Comprobante.Emitir(venta, pagoOtraVenta, "INT-001", 1));
    }

    [Fact]
    public void EmitirComprobante_ImportePagoNoCoincideConVenta_LanzaExcepcion()
    {
        var venta = new Venta(usuarioId: 1);
        AsignarIdVenta(venta, 1);
        venta.AgregarDetalle(productoId: 1, cantidad: 2, precioUnitario: 7.50m, nombreProducto: "Capuchino");
        venta.Confirmar("OP-002", cajaId: 1);

        var pagoIncoherente = Pago.RegistrarEfectivo(ventaId: venta.Id, cajaId: 1, usuarioId: 1, consumo: 10.00m, propina: 0m, recibido: 10.00m);

        Assert.Throws<InvalidOperationException>(() =>
            Comprobante.Emitir(venta, pagoIncoherente, "INT-001", 2));
    }

    [Fact]
    public void EmitirComprobante_PagoDeMismaVentaEImportesCoherentes_EmiteCorrectamente()
    {
        var venta = new Venta(usuarioId: 1);
        AsignarIdVenta(venta, 1);
        venta.AgregarDetalle(productoId: 1, cantidad: 2, precioUnitario: 7.50m, nombreProducto: "Capuchino");
        venta.Confirmar("OP-003", cajaId: 1);

        var pagoValido = Pago.RegistrarEfectivo(ventaId: venta.Id, cajaId: 1, usuarioId: 1, consumo: 15.00m, propina: 2.00m, recibido: 20.00m);
        var comprobante = Comprobante.Emitir(venta, pagoValido, "INT-001", 3);

        Assert.Equal(venta.Id, comprobante.VentaId);
        Assert.Equal(17.00m, comprobante.Total);
        Assert.Equal("INT-001-000003", comprobante.NumeroFormateado);
    }

    [Fact]
    public void AplicarDescuento_RedondeaDescuentoMonetarioAntesDeRestar_CasoCincoCentimosDiezPorCiento()
    {
        var venta = new Venta(usuarioId: 1);
        venta.AgregarDetalle(productoId: 1, cantidad: 1, precioUnitario: 0.05m, nombreProducto: "Producto prueba");

        venta.AplicarDescuento(10m);

        Assert.Equal(0.05m, venta.SubtotalBruto);
        Assert.Equal(0.04m, venta.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.50)]
    [InlineData(5.123)]
    [InlineData(0.005)]
    public void Producto_PrecioCeroNegativoOMasDeDosDecimales_LanzaExcepcion(decimal precioInvalido)
    {
        Assert.Throws<ArgumentException>(() => new Producto("Americano", precioInvalido));

        var producto = new Producto("Americano", 6.50m);
        Assert.Throws<ArgumentException>(() => producto.Precio = precioInvalido);
    }

    // --- Pruebas de las correcciones anteriores ---

    [Fact]
    public void Usuario_PasswordSeAlmacenaHasheadoYRechazaTextoPlano()
    {
        var usuario = Usuario.Crear("cajero1", "ClaveSegura123", RolUsuario.Cajero);

        Assert.NotEqual("ClaveSegura123", usuario.Password);
        Assert.Contains(":", usuario.Password);
        Assert.True(usuario.VerificarPassword("ClaveSegura123"));
        Assert.False(usuario.VerificarPassword("ClaveIncorrecta"));
    }

    [Fact]
    public void AgregarProducto_CantidadInvalidaEnProductoExistente_NoModificaCantidadPrevia()
    {
        var venta = new Venta(usuarioId: 1);
        var cafe = new Producto("Café", 5.00m, stock: 10) { Id = 1 };
        venta.AgregarProducto(cafe, 2);

        Assert.Throws<ArgumentException>(() => venta.AgregarProducto(cafe, 0));
        Assert.Throws<ArgumentException>(() => venta.AgregarProducto(cafe, -3));

        Assert.Single(venta.Detalles);
        Assert.Equal(2, venta.Detalles[0].Cantidad);
    }

    [Fact]
    public void Venta_DetallesSonSoloLecturaYNoPermitenEdicionExternaNiPostConfirmacion()
    {
        var venta = new Venta(usuarioId: 1);
        venta.AgregarDetalle(productoId: 1, cantidad: 2, precioUnitario: 5.00m, nombreProducto: "Café");

        Assert.False(venta.Detalles is List<DetalleVenta>);

        venta.Confirmar("OP-004", cajaId: 1);
        Assert.Throws<InvalidOperationException>(() => venta.ActualizarCantidadDetalle(1, 5));
        Assert.Throws<InvalidOperationException>(() => venta.QuitarDetalle(1));
    }
}