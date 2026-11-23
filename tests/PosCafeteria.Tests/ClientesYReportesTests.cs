using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;
using PosCafeteria.Infraestructura.Servicios;
using Xunit;

namespace PosCafeteria.Tests;

public class ClientesYReportesTests
{
    [Fact]
    public async Task GestionarCliente_ValidaDocumentoYConservaNombreHistoricoEnComprobante()
    {
        var almacen = new AlmacenPosEnMemoria();
        var admin = almacen.AgregarUsuario(Usuario.Crear("admin", "ClaveSegura123", RolUsuario.Administrador));
        var cafe = almacen.AgregarProducto(new Producto("Capuccino", 8.00m, stock: 10));

        var clientesSvc = new ServicioClientes(almacen);
        var cajaSvc = new ServicioCaja(almacen);
        var ventasSvc = new ServicioVentas(almacen);
        var compSvc = new ServicioComprobantes(almacen);

        // DNI inválido rechazado
        var dniInvalido = await clientesSvc.GestionarClienteAsync(
            new GestionarClienteRequest(null, "DNI", "123", "Ana Perez", null, null, null));
        Assert.False(dniInvalido.Exitoso);

        // Alta válida
        var alta = await clientesSvc.GestionarClienteAsync(
            new GestionarClienteRequest(null, "DNI", "12345678", "Ana Perez", "999888777", null, null));
        Assert.True(alta.Exitoso);

        int turnoId = (await cajaSvc.AbrirTurnoAsync(new AbrirTurnoRequest(admin.Id, 50m))).Valor;
        var borrador = (await ventasSvc.GuardarBorradorAsync(new GuardarBorradorRequest(
            null, admin.Id, alta.Valor!.Id, false, null, new List<LineaBorradorRequest> { new(cafe.Id, 1) }))).Valor!;

        var venta = (await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            Guid.NewGuid(), borrador.Id, admin.Id, turnoId, new DatosCobroRequest("Efectivo", 0m, 0m, 10m, false), null))).Valor!;

        // Editar nombre del cliente después de emitida la venta
        var edicion = await clientesSvc.GestionarClienteAsync(
            new GestionarClienteRequest(alta.Valor.Id, "DNI", "12345678", "Ana Perez Modificado", null, null, null));
        Assert.True(edicion.Exitoso);

        // El comprobante emitido conserva el nombre original
        var comp = (await compSvc.ObtenerComprobanteAsync(venta.VentaId)).Valor!;
        Assert.Contains("Ana Perez", comp.ContenidoResumen);
        Assert.DoesNotContain("Ana Perez Modificado", comp.ContenidoResumen);
    }

    [Fact]
    public async Task ConsultarReporte_ConciliaVentasConfirmadasAnuladasYRankingProductos()
    {
        var almacen = new AlmacenPosEnMemoria();
        var admin = almacen.AgregarUsuario(Usuario.Crear("admin", "ClaveSegura123", RolUsuario.Administrador));
        var cafe = almacen.AgregarProducto(new Producto("Espresso", 6.00m, stock: 20));
        var torta = almacen.AgregarProducto(new Producto("Torta Chocolate", 12.00m, stock: 20));

        var cajaSvc = new ServicioCaja(almacen);
        var ventasSvc = new ServicioVentas(almacen);
        var reporteSvc = new ServicioReportes(almacen);

        int turnoId = (await cajaSvc.AbrirTurnoAsync(new AbrirTurnoRequest(admin.Id, 100m))).Valor;

        // Venta 1: 2 Espressos (S/ 12) + S/ 2 propina en Efectivo (vigente)
        var b1 = (await ventasSvc.GuardarBorradorAsync(new GuardarBorradorRequest(
            null, admin.Id, null, false, null, new List<LineaBorradorRequest> { new(cafe.Id, 2) }))).Valor!;
        await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            Guid.NewGuid(), b1.Id, admin.Id, turnoId, new DatosCobroRequest("Efectivo", 0m, 2m, 20m, false), null));

        // Venta 2: 1 Torta (S/ 12) en YapePlin (luego anulada)
        var b2 = (await ventasSvc.GuardarBorradorAsync(new GuardarBorradorRequest(
            null, admin.Id, null, false, null, new List<LineaBorradorRequest> { new(torta.Id, 1) }))).Valor!;
        var v2 = (await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            Guid.NewGuid(), b2.Id, admin.Id, turnoId, new DatosCobroRequest("YapePlin", 0m, 0m, 12m, true), null))).Valor!;

        await cajaSvc.AnularVentaAsync(new AnularVentaRequest(
            Guid.NewGuid(), v2.VentaId, turnoId, new AutorizacionAdminRequest("admin", "ClaveSegura123", "Devolución"), true));

        var resReporte = await reporteSvc.ConsultarReporteAsync(new FiltroReporteRequest(
            DateTime.Now.AddHours(-1),
            DateTime.Now.AddHours(1),
            turnoId,
            null,
            null));

        Assert.True(resReporte.Exitoso);
        Assert.Equal(24.00m, resReporte.Valor!.BrutoConfirmado);
        Assert.Equal(12.00m, resReporte.Valor.Reembolsos);
        Assert.Equal(12.00m, resReporte.Valor.Neto);
        Assert.Equal(2.00m, resReporte.Valor.Propinas);
        Assert.Equal(1, resReporte.Valor.CantidadVentas);
        Assert.Single(resReporte.Valor.ProductosMasVendidos);
        Assert.Equal("Espresso", resReporte.Valor.ProductosMasVendidos[0].NombreProducto);
        Assert.Equal(2, resReporte.Valor.ProductosMasVendidos[0].UnidadesVendidas);
    }
}