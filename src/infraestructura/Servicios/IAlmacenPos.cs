using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PosCafeteria.Dominio;

namespace PosCafeteria.Infraestructura.Servicios;

public interface IAlmacenPos
{
    Task EjecutarEnTransaccionAsync(Func<Task> accion);

    // Usuarios
    Task<Usuario?> ObtenerUsuarioPorIdAsync(int usuarioId);
    Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario);
    Task<IReadOnlyList<Usuario>> ListarUsuariosAsync();
    Task AgregarUsuarioAsync(Usuario usuario);

    // Catálogo e Inventario
    Task<IReadOnlyList<Producto>> ListarProductosAsync();
    Task<IReadOnlyDictionary<int, string>> ObtenerNombresCategoriasAsync();
    Task<Producto?> ObtenerProductoPorIdAsync(int productoId);
    Task AgregarMovimientoStockAsync(MovimientoStock movimiento);

    // Caja y Movimientos
    Task<Caja?> ObtenerCajaPorIdAsync(int cajaId);
    Task<Caja?> ObtenerCajaAbiertaPorUsuarioAsync(int usuarioId);
    Task<IReadOnlyList<CajaMovimiento>> ListarMovimientosCajaAsync(int cajaId);
    Task AgregarCajaAsync(Caja caja);
    Task AgregarMovimientoCajaAsync(CajaMovimiento movimiento);

    // Ventas, Borradores, Pagos y Comprobantes
    Task<Venta?> ObtenerVentaPorIdAsync(int ventaId);
    Task<Venta?> ObtenerVentaPorOperacionAsync(string identificadorOperacion);
    Task<IReadOnlyList<Venta>> ListarBorradoresPorUsuarioAsync(int usuarioId);
    Task AgregarVentaAsync(Venta venta);
    Task<Pago?> ObtenerPagoPorVentaIdAsync(int ventaId);
    Task AgregarPagoAsync(Pago pago);
    Task<Comprobante?> ObtenerComprobantePorVentaIdAsync(int ventaId);
    Task<int> ObtenerSiguienteNumeroComprobanteAsync(string serie);
    Task AgregarComprobanteAsync(Comprobante comprobante);

    // Clientes
    Task<Cliente?> ObtenerClientePorIdAsync(int clienteId);
    Task<IReadOnlyList<Cliente>> ListarClientesAsync();
    Task AgregarClienteAsync(Cliente cliente);

    // Consultas para Reportes
    Task<IReadOnlyList<Venta>> ListarVentasAsync();
    Task<IReadOnlyList<Pago>> ListarPagosAsync();
    
    // Idempotencia de Anulaciones
    Task<int?> ObtenerVentaIdPorOperacionAnulacionAsync(string identificadorOperacion);
    Task RegistrarOperacionAnulacionAsync(string identificadorOperacion, int ventaId);
}