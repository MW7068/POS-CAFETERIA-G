using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

public class Pbi03CatalogoQaTests
{
    [Theory]
    [InlineData("", 3.00)]
    [InlineData("   ", 3.00)]
    [InlineData("Agua", 0.00)]
    [InlineData("Agua", -1.00)]
    [InlineData("Agua", 3.125)]
    public void CrearProducto_ConDatosInvalidos_RechazaSinPersistir(string nombre, decimal precio)
    {
        Assert.Throws<ArgumentException>(() => new Producto(nombre, precio));

        using var conexion = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        conexion.Open();

        var opciones = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new CafeteriaDbContext(opciones);
        contexto.Database.Migrate();

        Assert.Empty(contexto.Productos.ToList());
    }

    [Fact]
    public void CambiarPrecio_ConRolCajero_DeniegaModificacionYConservaPrecio()
    {
        var cajero = Usuario.Crear("cajero01", "ClaveSegura123", RolUsuario.Cajero);
        var producto = new Producto("Cafe Americano", 5.00m, stock: 10, stockMinimo: 2);

        Assert.Equal(RolUsuario.Cajero, cajero.Rol);
        Assert.True(cajero.Activo);

        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
        {
            if (cajero.Rol != RolUsuario.Administrador || !cajero.Activo)
                throw new UnauthorizedAccessException("Solo el rol Administrador puede modificar precios.");

            producto.Precio = 9.50m;
        });

        Assert.NotEmpty(ex.Message);
        Assert.Equal(5.00m, producto.Precio);
    }

    [Fact]
    public void RegistrarCambioPrecioYReiniciar_EnArchivoTemporal_ConservaDatosYNoDuplica()
    {
        string rutaArchivo = Path.Combine(
            Path.GetTempPath(),
            $"pos_pbi03_qa_{Guid.NewGuid():N}.db");

        try
        {
            int productoId;

            using (var ctx1 = ConexionSqlite.CrearContexto(rutaArchivo, cargarDemo: false))
            {
                ctx1.Database.Migrate();

                var categoria = new Categoria("Bebidas");
                ctx1.Categorias.Add(categoria);
                ctx1.SaveChanges();

                var producto = new Producto("Agua Sin Gas", 3.00m, stock: 0, stockMinimo: 0, categoriaId: categoria.Id);
                ctx1.Productos.Add(producto);
                ctx1.SaveChanges();
                productoId = producto.Id;
            }

            // Simula cierre y reapertura del programa sobre la misma base SQLite.
            using (var ctx2 = ConexionSqlite.CrearContexto(rutaArchivo, cargarDemo: false))
            {
                ctx2.Database.Migrate();

                var productoRecuperado = ctx2.Productos.Single(p => p.Id == productoId);
                Assert.Equal("Agua Sin Gas", productoRecuperado.Nombre);
                Assert.Equal(3.00m, productoRecuperado.Precio);
                Assert.Equal(0, productoRecuperado.Stock);
                Assert.True(productoRecuperado.Activo);

                productoRecuperado.Precio = 3.50m;
                ctx2.SaveChanges();
            }

            // Segunda reapertura para verificar persistencia del cambio de precio.
            using (var ctx3 = ConexionSqlite.CrearContexto(rutaArchivo, cargarDemo: false))
            {
                ctx3.Database.Migrate();

                var productos = ctx3.Productos.AsNoTracking().ToList();
                Assert.Single(productos);
                Assert.Equal(3.50m, productos[0].Precio);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(rutaArchivo))
                File.Delete(rutaArchivo);
        }
    }
}
