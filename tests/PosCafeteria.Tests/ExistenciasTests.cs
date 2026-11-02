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
        venta.AgregarProducto(sandwich, 2); // Excede el stock disponible de 1

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
        venta.AgregarProducto(agua, 1); // Suma 3 acumulados

        Assert.Throws<InvalidOperationException>(() => venta.Confirmar(catalogo));
        Assert.Equal(2, agua.Existencias);
    }

    [Fact]
    public void AgregarProductos_SinConfirmar_NoModificaExistencias()
    {
        var agua = new Producto(1, "Agua", 2.50m, 10);

        var venta = new Venta();
        venta.AgregarProducto(agua, 3);

        Assert.Equal(10, agua.Existencias);
        Assert.False(venta.EstaConfirmada);
    }
}