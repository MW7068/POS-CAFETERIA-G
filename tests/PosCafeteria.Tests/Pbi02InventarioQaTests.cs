using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;
using PosCafeteria.Infraestructura.Persistencia;
using PosCafeteria.Infraestructura.Repositorios;
using Xunit;

namespace PosCafeteria.Tests;

public class Pbi02InventarioQaTests : IDisposable
{
    private readonly SqliteConnection _conexionMemoria;
    private readonly DbContextOptions<CafeteriaDbContext> _opcionesMemoria;

    public Pbi02InventarioQaTests()
    {
        _conexionMemoria = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _conexionMemoria.Open();

        _opcionesMemoria = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(_conexionMemoria)
            .Options;

        using var contexto = new CafeteriaDbContext(_opcionesMemoria);
        contexto.Database.Migrate();
    }

    private CafeteriaDbContext CrearContextoMemoria() => new(_opcionesMemoria);

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void DetalleVenta_CantidadNoPositiva_LanzaExcepcion(int cantidadInvalida)
    {
        Assert.Throws<ArgumentException>(() => new DetalleVenta(1, "Producto Prueba", cantidadInvalida, 5.00m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.50)]
    public void DetalleVenta_PrecioNoPositivo_LanzaExcepcion(decimal precioInvalido)
    {
        Assert.Throws<ArgumentException>(() => new DetalleVenta(1, "Producto Prueba", 2, precioInvalido));
    }

    [Fact]
    public void Venta_AgregarMismoProductoVariasVeces_AcumulaCantidadYTotal()
    {
        var venta = new Venta();
        venta.AgregarProducto(10, 2, 4.50m);
        venta.AgregarProducto(10, 3, 4.50m);

        var detalle = Assert.Single(venta.Detalles);
        Assert.Equal(5, detalle.Cantidad);
        Assert.Equal(22.50m, venta.Total);
    }

    [Fact]
    public void GuardarVenta_ConFalloEnSegundoProducto_HaceRollbackSinDejarDatosParciales()
    {
        // Entrega parcial: verifica que no queden ventas ni detalles ante un producto inexistente;
        // la reversion de descuento de stock queda pendiente hasta contar con el flujo real de confirmacion.
        int productoValidoId;

        using (var prep = CrearContextoMemoria())
        {
            var cafe = new Producto
            {
                Nombre = "Cafe Pasado",
                Precio = 6.00m,
                Stock = 15
            };
            prep.Productos.Add(cafe);
            prep.SaveChanges();
            productoValidoId = cafe.Id;
        }

        using (var ctx = CrearContextoMemoria())
        {
            var venta = new Venta();
            venta.AgregarProducto(productoValidoId, 2, 6.00m);
            venta.AgregarProducto(999999, 1, 4.00m);

            var repo = new RepositorioVentas(ctx);
            Assert.Throws<DbUpdateException>(() => repo.Guardar(venta));
        }

        using var verificacion = CrearContextoMemoria();
        Assert.Empty(verificacion.Ventas);
        Assert.Empty(verificacion.DetallesVenta);
        var productoIntacto = verificacion.Productos.Single(p => p.Id == productoValidoId);
        Assert.Equal(15, productoIntacto.Stock);
    }

    [Fact]
    public void PersistenciaEnArchivoSqlite_TrasCerrarYReabrirContexto_ConservaDatos()
    {
        string carpetaBaseCompartida = Path.Combine(Path.GetTempPath(), "CafeMamaOli_QA");
        string carpetaEjecucionUnica = Path.Combine(carpetaBaseCompartida, $"run_{Guid.NewGuid():N}");
        Directory.CreateDirectory(carpetaEjecucionUnica);
        string rutaTemp = Path.Combine(carpetaEjecucionUnica, "qa_pbi02.db");

        try
        {
            int ventaId;

            using (var ctx1 = ConexionSqlite.CrearContexto(rutaTemp))
            {
                ctx1.Database.Migrate();

                var infusion = new Producto
                {
                    Nombre = "Mate de Coca",
                    Precio = 3.50m,
                    Stock = 25
                };
                ctx1.Productos.Add(infusion);
                ctx1.SaveChanges();

                var venta = new Venta();
                venta.AgregarProducto(infusion, 4);

                ventaId = new RepositorioVentas(ctx1).Guardar(venta);
            }

            using var ctx2 = ConexionSqlite.CrearContexto(rutaTemp);
            var repo2 = new RepositorioVentas(ctx2);
            var recuperada = repo2.ObtenerPorId(ventaId);

            Assert.NotNull(recuperada);
            Assert.Equal(14.00m, recuperada.Total);
            var detalle = Assert.Single(recuperada.Detalles);
            Assert.Equal(4, detalle.Cantidad);
            Assert.Equal(3.50m, detalle.PrecioUnitario);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(rutaTemp))
            {
                try { File.Delete(rutaTemp); } catch { }
            }
            if (Directory.Exists(carpetaEjecucionUnica))
            {
                try { Directory.Delete(carpetaEjecucionUnica, recursive: false); } catch { }
            }
        }
    }

    public void Dispose()
    {
        _conexionMemoria.Dispose();
    }
}
