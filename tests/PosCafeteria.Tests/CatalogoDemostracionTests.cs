using System.Linq;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

public class CatalogoDemostracionTests
{
    [Fact]
    public void CargarDemo_CreaProductosConPreciosYStockIniciales()
    {
        using var contexto = ConexionSqlite.CrearContexto(
            ":memory:",
            cargarDemo: true);

        contexto.Database.OpenConnection();
        contexto.Database.Migrate();

        var productos = contexto.Productos.AsNoTracking().ToList();

        Assert.Equal(2, productos.Count);

        var cafe = productos.Single(p => p.Nombre == "Café");
        var sandwich = productos.Single(p => p.Nombre == "Sándwich");

        Assert.True(cafe.Id > 0);
        Assert.True(sandwich.Id > 0);

        Assert.Equal(5.00m, cafe.Precio);
        Assert.Equal(0, cafe.Stock);

        Assert.Equal(8.00m, sandwich.Precio);
        Assert.Equal(0, sandwich.Stock);
    }

[Fact]
public void RepetirCarga_NoDuplicaNiSobrescribeProductosExistentes()
{
    using var contexto = ConexionSqlite.CrearContexto(
        ":memory:",
        cargarDemo: true);

    contexto.Database.OpenConnection();
    contexto.Database.Migrate();

    var cafe = contexto.Productos.Single(p => p.Nombre == "Café");
    var cafeId = cafe.Id;

    // Preparamos un producto existente con datos modificados.
    cafe.Precio = 6.00m;
    cafe.Stock = 17;

    contexto.SaveChanges();
    contexto.ChangeTracker.Clear();

    // Ejecutamos nuevamente la carga del catálogo.
    contexto.Database.Migrate();

    var productos = contexto.Productos.AsNoTracking().ToList();
    var recuperado = productos.Single(p => p.Nombre == "Café");

    // No debe duplicar productos.
    Assert.Equal(2, productos.Count);

    // Debe conservar el mismo identificador.
    Assert.Equal(cafeId, recuperado.Id);

    // Debe conservar el precio modificado.
    Assert.Equal(6.00m, recuperado.Precio);

    // No debe sobrescribir el stock existente.
    Assert.Equal(17, recuperado.Stock);
}


}