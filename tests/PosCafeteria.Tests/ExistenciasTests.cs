using System;
using System.Collections.Generic;
using PosCafeteria.Dominio;
using Xunit;

namespace PosCafeteria.Tests;

public class ExistenciasTests
{
    [Fact]
    public void ConfirmarVenta_DescuentaExistenciasCorrectamente()
    {
        var agua = new Producto(1, "Agua", 2.50m, 10);
        var catalogo = new Dictionary<int, Producto> { { 1, agua } };

        var venta = new Venta();
        venta.AgregarProducto(agua, 3);
        venta.Confirmar(catalogo);

        Assert.Equal(7, agua.Existencias);
        Assert.True(venta.EstaConfirmada);
    }

    [Fact]
    public void ConfirmarVenta_SinStockSuficiente_LanzaExcepcionYNoModificaStock()
    {
        var agua = new Producto(1, "Agua", 2.50m, 2);
        var catalogo = new Dictionary<int, Producto> { { 1, agua } };

        var venta = new Venta();
        venta.AgregarProducto(agua, 3);

        Assert.Throws<InvalidOperationException>(() => venta.Confirmar(catalogo));
        Assert.Equal(2, agua.Existencias);
        Assert.False(venta.EstaConfirmada);
    }

    [Fact]
    public void ConfirmarVenta_FalloEnUnProducto_NoDescuentaOtrosProductos()
    {
        var agua = new Producto(1, "Agua", 2.50m, 10);
        var sandwich = new Producto(2, "Sandwich", 8.00m, 1);
        var catalogo = new Dictionary<int, Producto> { { 1, agua }, { 2, sandwich } };

        var venta = new Venta();
        venta.AgregarProducto(agua, 2);
        venta.AgregarProducto(sandwich, 2);

        Assert.Throws<InvalidOperationException>(() => venta.Confirmar(catalogo));
        Assert.Equal(10, agua.Existencias);
        Assert.Equal(1, sandwich.Existencias);
    }

    [Fact]
    public void ConfirmarVenta_ValidacionCantidadAcumulada_RechazaSiSuperaStock()
    {
        var agua = new Producto(1, "Agua", 2.50m, 2);
        var catalogo = new Dictionary<int, Producto> { { 1, agua } };

        var venta = new Venta();
        venta.AgregarProducto(agua, 2);
        venta.AgregarProducto(agua, 1);

        Assert.Throws<InvalidOperationException>(() => venta.Confirmar(catalogo));
        Assert.Equal(2, agua.Existencias);
    }

    [Fact]
    public void DescontarStock_CantidadCeroONegativa_LanzaExcepcionYNoModificaStock()
    {
        var agua = new Producto(1, "Agua", 2.50m, 10);

        Assert.Throws<ArgumentException>(() => agua.DescontarStock(0));
        Assert.Throws<ArgumentException>(() => agua.DescontarStock(-5));
        Assert.Equal(10, agua.Existencias);
    }

    [Fact]
    public void ConfirmarVenta_VentaVacia_LanzaExcepcion()
    {
        var catalogo = new Dictionary<int, Producto>();
        var venta = new Venta();

        Assert.Throws<InvalidOperationException>(() => venta.Confirmar(catalogo));
        Assert.False(venta.EstaConfirmada);
    }

    [Fact]
    public void ConfirmarVenta_ConfirmacionRepetida_LanzaExcepcion()
    {
        var agua = new Producto(1, "Agua", 2.50m, 10);
        var catalogo = new Dictionary<int, Producto> { { 1, agua } };

        var venta = new Venta();
        venta.AgregarProducto(agua, 2);
        venta.Confirmar(catalogo);

        Assert.Throws<InvalidOperationException>(() => venta.Confirmar(catalogo));
        Assert.Equal(8, agua.Existencias);
    }

    [Fact]
    public void AgregarProducto_VentaYaConfirmada_LanzaExcepcionYNoModificaTotales()
    {
        var agua = new Producto(1, "Agua", 2.50m, 10);
        var catalogo = new Dictionary<int, Producto> { { 1, agua } };

        var venta = new Venta();
        venta.AgregarProducto(agua, 2);
        venta.Confirmar(catalogo);

        decimal totalInicial = venta.Total;
        int detallesInicial = venta.Detalles.Count;

        Assert.Throws<InvalidOperationException>(() => venta.AgregarProducto(agua, 1));
        Assert.Equal(totalInicial, venta.Total);
        Assert.Equal(detallesInicial, venta.Detalles.Count);
        Assert.Equal(8, agua.Existencias);
    }

    [Fact]
    public void CrearProductoODetalle_PrecioMenorOIgualACero_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() => new Producto(1, "Agua", 0m, 10));
        Assert.Throws<ArgumentException>(() => new Producto(1, "Agua", -2.50m, 10));
        Assert.Throws<ArgumentException>(() => new DetalleVenta(1, 2, 0m));
        Assert.Throws<ArgumentException>(() => new DetalleVenta(1, 2, -1.50m));
    }
}
