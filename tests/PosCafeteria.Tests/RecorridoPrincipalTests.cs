using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;
using PosCafeteria.Infraestructura.Servicios;
using Xunit;

namespace PosCafeteria.Tests;

public class RecorridoPrincipalTests
{
    private static (AlmacenPosEnMemoria Almacen, Usuario Admin, Producto Cafe, ServicioCatalogoInventario CatalogoInv, ServicioCaja CajaSvc, ServicioVentas VentasSvc, ServicioComprobantes CompSvc)
        CrearEscenarioBase()
    {
        var almacen = new AlmacenPosEnMemoria();
        var admin = almacen.AgregarUsuario(Usuario.Crear("admin", "ClaveSegura123", RolUsuario.Administrador));
        almacen.AgregarCategoria(1, "Bebidas");
        var cafe = almacen.AgregarProducto(new Producto("Café Americano", 5.00m, stock: 0, stockMinimo: 2, categoriaId: 1));

        return (
            almacen,
            admin,
            cafe,
            new ServicioCatalogoInventario(almacen),
            new ServicioCaja(almacen),
            new ServicioVentas(almacen),
            new ServicioComprobantes(almacen));
    }

    [Fact]
    public async Task RecorridoVerticalCompleto_CajaVentaCobroInventarioComprobanteYCierre_CalculaExacto()
    {
        var (_, admin, cafe, catalogoInv, cajaSvc, ventasSvc, compSvc) = CrearEscenarioBase();

        // 1. Reponer 10 unidades del producto a S/ 5.00
        var resRepo = await catalogoInv.RegistrarMovimientoStockAsync(
            new MovimientoStockRequest(cafe.Id, "Entrada", 10, "Reposición inicial", admin.Id));
        Assert.True(resRepo.Exitoso);
        Assert.Equal(10, cafe.Stock);

        // 2. Abrir caja con S/ 50.00
        var resTurno = await cajaSvc.AbrirTurnoAsync(new AbrirTurnoRequest(admin.Id, 50.00m));
        Assert.True(resTurno.Exitoso);
        int turnoId = resTurno.Valor;

        // 3. Guardar borrador con 2 unidades y confirmar cobrando S/ 20.00 en efectivo
        var resBorrador = await ventasSvc.GuardarBorradorAsync(new GuardarBorradorRequest(
            null,
            admin.Id,
            null,
            false,
            null,
            new List<LineaBorradorRequest> { new(cafe.Id, 2) }));
        Assert.True(resBorrador.Exitoso);

        var opVenta = Guid.NewGuid();
        var resConfirm = await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            opVenta,
            resBorrador.Valor!.Id,
            admin.Id,
            turnoId,
            new DatosCobroRequest("Efectivo", 0m, 0m, 20.00m, false),
            null));

        Assert.True(resConfirm.Exitoso);
        Assert.Equal(10.00m, resConfirm.Valor!.ConsumoNeto);
        Assert.Equal(10.00m, resConfirm.Valor.Vuelto);
        Assert.Equal("INT-001-000001", resConfirm.Valor.SerieNumeroComprobante);
        Assert.Equal(8, cafe.Stock);

        // 4. Reintentar confirmación con el mismo Guid no duplica venta ni descuenta stock
        var resReintento = await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            opVenta,
            resBorrador.Valor.Id,
            admin.Id,
            turnoId,
            new DatosCobroRequest("Efectivo", 0m, 0m, 20.00m, false),
            null));

        Assert.True(resReintento.Exitoso);
        Assert.Equal(resConfirm.Valor.VentaId, resReintento.Valor!.VentaId);
        Assert.Equal(8, cafe.Stock);

        // 5. Consultar comprobante interno emitido
        var resComp = await compSvc.ObtenerComprobanteAsync(resConfirm.Valor.VentaId);
        Assert.True(resComp.Exitoso);
        Assert.Equal(Comprobante.Leyenda, resComp.Valor!.RotuloLegal);
        Assert.Equal("EMITIDO", resComp.Valor.Estado);

        // 6. Cerrar caja: esperado debe ser S/ 60.00 (50 inicial + 10 venta)
        var resCierre = await cajaSvc.CerrarTurnoAsync(new CerrarTurnoRequest(turnoId, admin.Id, 60.00m));
        Assert.True(resCierre.Exitoso);
        Assert.Equal(60.00m, resCierre.Valor!.EfectivoEsperado);
        Assert.Equal(0.00m, resCierre.Valor.Diferencia);
    }

    [Fact]
    public async Task AnularVenta_RevierteStockYCajaUnaSolaVez_YRechazaSegundaAnulacion()
    {
        var (_, admin, cafe, catalogoInv, cajaSvc, ventasSvc, compSvc) = CrearEscenarioBase();
        await catalogoInv.RegistrarMovimientoStockAsync(new MovimientoStockRequest(cafe.Id, "Entrada", 10, "Stock inicial", admin.Id));
        int turnoId = (await cajaSvc.AbrirTurnoAsync(new AbrirTurnoRequest(admin.Id, 50.00m))).Valor;

        var borrador = (await ventasSvc.GuardarBorradorAsync(new GuardarBorradorRequest(
            null, admin.Id, null, false, null, new List<LineaBorradorRequest> { new(cafe.Id, 2) }))).Valor!;

        var ventaConf = (await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            Guid.NewGuid(), borrador.Id, admin.Id, turnoId, new DatosCobroRequest("Efectivo", 0m, 0m, 10.00m, false), null))).Valor!;

        Assert.Equal(8, cafe.Stock);

        var auth = new AutorizacionAdminRequest("admin", "ClaveSegura123", "Error en pedido del cliente");
        var opAnulacion = Guid.NewGuid();

        var resAnular1 = await cajaSvc.AnularVentaAsync(new AnularVentaRequest(opAnulacion, ventaConf.VentaId, turnoId, auth, false));
        Assert.True(resAnular1.Exitoso);
        Assert.Equal(10, cafe.Stock);

        // Segunda anulación con distinto Guid debe rechazarse
        var resAnular2 = await cajaSvc.AnularVentaAsync(new AnularVentaRequest(Guid.NewGuid(), ventaConf.VentaId, turnoId, auth, false));
        Assert.False(resAnular2.Exitoso);
        Assert.Equal(CodigosErrorPos.EstadoInvalido, resAnular2.Error!.Codigo);
        Assert.Equal(10, cafe.Stock);

        var compAnulado = await compSvc.ObtenerComprobanteAsync(ventaConf.VentaId);
        Assert.Equal("ANULADO", compAnulado.Valor!.Estado);

        var cierre = (await cajaSvc.CerrarTurnoAsync(new CerrarTurnoRequest(turnoId, admin.Id, 50.00m))).Valor!;
        Assert.Equal(50.00m, cierre.EfectivoEsperado);
    }

    [Fact]
    public async Task CobroDigitalManual_NoSumaAlEfectivoEsperadoDeGaveta()
    {
        var (_, admin, cafe, catalogoInv, cajaSvc, ventasSvc, _) = CrearEscenarioBase();
        await catalogoInv.RegistrarMovimientoStockAsync(new MovimientoStockRequest(cafe.Id, "Entrada", 5, "Stock", admin.Id));
        int turnoId = (await cajaSvc.AbrirTurnoAsync(new AbrirTurnoRequest(admin.Id, 50.00m))).Valor;

        var borrador = (await ventasSvc.GuardarBorradorAsync(new GuardarBorradorRequest(
            null, admin.Id, null, true, null, new List<LineaBorradorRequest> { new(cafe.Id, 2) }))).Valor!;

        // Sin confirmación digital manual debe fallar
        var falloDigital = await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            Guid.NewGuid(), borrador.Id, admin.Id, turnoId, new DatosCobroRequest("YapePlin", 0m, 0m, 10.00m, false), null));
        Assert.False(falloDigital.Exitoso);
        Assert.Equal(CodigosErrorPos.Validacion, falloDigital.Error!.Codigo);

        // Con confirmación digital manual debe aprobar y quedar fuera de gaveta
        var okDigital = await ventasSvc.ConfirmarVentaAsync(new ConfirmarVentaRequest(
            Guid.NewGuid(), borrador.Id, admin.Id, turnoId, new DatosCobroRequest("YapePlin", 0m, 0m, 10.00m, true), null));
        Assert.True(okDigital.Exitoso);

        var cierre = (await cajaSvc.CerrarTurnoAsync(new CerrarTurnoRequest(turnoId, admin.Id, 50.00m))).Valor!;
        Assert.Equal(50.00m, cierre.EfectivoEsperado);
        Assert.Equal(10.00m, cierre.TotalDigitalFueraGaveta);
    }
}