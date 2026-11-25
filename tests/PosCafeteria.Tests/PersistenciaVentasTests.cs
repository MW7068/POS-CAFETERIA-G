using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;
using PosCafeteria.Infraestructura.Persistencia;
using PosCafeteria.Infraestructura.Repositorios;
using Xunit;

namespace PosCafeteria.Tests;

public class PersistenciaVentasTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<CafeteriaDbContext> _opciones;
    private readonly int _usuarioId;

    public PersistenciaVentasTests()
    {
        _conexion = new SqliteConnection(
            "Data Source=:memory:;Foreign Keys=True");

        _conexion.Open();

        _opciones = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = CrearContexto();
        contexto.Database.Migrate();

        var usuario = Usuario.Crear(
            "cajero_pruebas",
            "ClaveSoloParaTests123!",
            RolUsuario.Cajero);

        contexto.Usuarios.Add(usuario);
        contexto.SaveChanges();

        _usuarioId = usuario.Id;
    }

    private CafeteriaDbContext CrearContexto()
    {
        return new CafeteriaDbContext(_opciones);
    }

    [Fact]
    public void Guardar_PermiteRecuperarVentaConSusDetalles()
    {
        int ventaId;
        int cafeId;
        int panId;
        DateTime fecha;

        using (var contexto = CrearContexto())
        {
            var cafe = new Producto
            {
                Nombre = "Cafe",
                Precio = 7.50m,
                Stock = 20
            };

            var pan = new Producto
            {
                Nombre = "Pan",
                Precio = 3.25m,
                Stock = 30
            };

            contexto.Productos.AddRange(cafe, pan);
            contexto.SaveChanges();

            cafeId = cafe.Id;
            panId = pan.Id;

            var venta = new Venta(_usuarioId);
            venta.AgregarProducto(cafe, 2);
            venta.AgregarProducto(pan, 3);
            fecha = venta.Fecha;

            var repositorio = new RepositorioVentas(contexto);
            ventaId = repositorio.Guardar(venta);
        }

        using var lectura = CrearContexto();
        var recuperada = new RepositorioVentas(lectura)
            .ObtenerPorId(ventaId);

        Assert.NotNull(recuperada);
        Assert.True(ventaId > 0);
        Assert.Equal(ventaId, recuperada.Id);
        Assert.Equal(fecha, recuperada.Fecha);
        Assert.Equal(2, recuperada.Detalles.Count);
        Assert.Equal(24.75m, recuperada.Total);

        var detalleCafe = recuperada.Detalles
            .Single(d => d.ProductoId == cafeId);

        var detallePan = recuperada.Detalles
            .Single(d => d.ProductoId == panId);

        Assert.True(detalleCafe.Id > 0);
        Assert.True(detallePan.Id > 0);
        Assert.NotEqual(detalleCafe.Id, detallePan.Id);

        Assert.Equal(2, detalleCafe.Cantidad);
        Assert.Equal(7.50m, detalleCafe.PrecioUnitario);

        Assert.Equal(3, detallePan.Cantidad);
        Assert.Equal(3.25m, detallePan.PrecioUnitario);
    }

    [Fact]
    public void CambiarPrecioDelProducto_ConservaPrecioHistorico()
    {
        int productoId;
        int ventaId;

        using (var contexto = CrearContexto())
        {
            var producto = new Producto
            {
                Nombre = "Cafe",
                Precio = 6.00m,
                Stock = 20
            };

            contexto.Productos.Add(producto);
            contexto.SaveChanges();
            productoId = producto.Id;

            var venta = new Venta(_usuarioId);
            venta.AgregarProducto(producto, 2);

            ventaId = new RepositorioVentas(contexto)
                .Guardar(venta);
        }

        using (var actualizacion = CrearContexto())
        {
            var producto = actualizacion.Productos
                .Single(p => p.Id == productoId);

            producto.Precio = 9.00m;
            actualizacion.SaveChanges();
        }

        using var lectura = CrearContexto();

        var recuperada = new RepositorioVentas(lectura)
            .ObtenerPorId(ventaId);

        Assert.NotNull(recuperada);

        var detalle = Assert.Single(recuperada.Detalles);

        Assert.Equal(6.00m, detalle.PrecioUnitario);
        Assert.Equal(12.00m, recuperada.Total);

        var precioActual = lectura.Productos
            .Single(p => p.Id == productoId)
            .Precio;

        Assert.Equal(9.00m, precioActual);
    }

    [Fact]
    public void Guardar_ConProductoInexistente_NoDejaRegistrosParciales()
    {
        int productoId;

        using (var preparacion = CrearContexto())
        {
            var producto = new Producto
            {
                Nombre = "Cafe",
                Precio = 5.00m,
                Stock = 10
            };

            preparacion.Productos.Add(producto);
            preparacion.SaveChanges();
            productoId = producto.Id;
        }

        using (var contexto = CrearContexto())
        {
            var venta = new Venta(_usuarioId);

            venta.AgregarProducto(productoId, 2, 5.00m);
            venta.AgregarProducto(999999, 1, 3.00m);

            var repositorio = new RepositorioVentas(contexto);

            var error = Assert.Throws<DbUpdateException>(
                () => repositorio.Guardar(venta));

            var errorSqlite = Assert.IsType<SqliteException>(
                error.InnerException);

            // SQLITE_CONSTRAINT_FOREIGNKEY.
            Assert.Equal(787, errorSqlite.SqliteExtendedErrorCode);
        }

        using var lectura = CrearContexto();

        Assert.Empty(lectura.Ventas);
        Assert.Empty(lectura.DetallesVenta);
        Assert.Single(lectura.Productos);
    }


    [Fact]
    public void Transaccion_SiFallaSegundaVenta_ReviertePrimeraVenta()
    {
        int productoId;

        // 1. Preparamos un producto que sí debe conservarse.
        using (var preparacion = CrearContexto())
        {
            var producto = new Producto
            {
                Nombre = "Cafe",
                Precio = 5.00m,
                Stock = 10
            };

            preparacion.Productos.Add(producto);
            preparacion.SaveChanges();
            productoId = producto.Id;
        }

        // 2. Iniciamos una transacción SQLite real.
        using (var contexto = CrearContexto())
        {
            using var transaccion = contexto.Database.BeginTransaction();

            // Primera venta: se guarda correctamente.
            var primeraVenta = new Venta(_usuarioId);
            primeraVenta.AgregarProducto(productoId, 2, 5.00m);

            var repositorio = new RepositorioVentas(contexto);
            repositorio.Guardar(primeraVenta);

            Assert.True(primeraVenta.Id > 0);
            Assert.Single(contexto.Ventas);

            // Segunda venta: referencia un producto inexistente.
            var segundaVenta = new Venta(_usuarioId);
            segundaVenta.AgregarProducto(999999, 1, 3.00m);

            Assert.Throws<DbUpdateException>(
                () => repositorio.Guardar(segundaVenta));

            // 3. Revertimos toda la operación.
            transaccion.Rollback();
        }

        // 4. Comprobamos el resultado desde otro DbContext.
        using (var lectura = CrearContexto())
        {
            Assert.Empty(lectura.Ventas);
            Assert.Empty(lectura.DetallesVenta);

            // Los datos anteriores a la transacción permanecen.
            var producto = Assert.Single(lectura.Productos);
            Assert.Equal(productoId, producto.Id);
            Assert.Equal(10, producto.Stock);
        }
    }




    public void Dispose()
    {
        _conexion.Dispose();
    }




}