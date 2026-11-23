using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PosCafeteria.Dominio;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class AlmacenPosEnMemoria : IAlmacenPos
{
    private readonly List<Usuario> _usuarios = new();
    private readonly Dictionary<int, string> _categorias = new();
    private readonly List<Producto> _productos = new();
    private readonly List<Cliente> _clientes = new();
    private readonly List<MovimientoStock> _movimientosStock = new();
    private readonly List<Caja> _cajas = new();
    private readonly List<CajaMovimiento> _movimientosCaja = new();
    private readonly List<Venta> _ventas = new();
    private readonly List<Pago> _pagos = new();
    private readonly List<Comprobante> _comprobantes = new();
    private readonly Dictionary<string, int> _operacionesAnulacion = new(StringComparer.OrdinalIgnoreCase);

    private int _seqUsuario = 1;
    private int _seqProducto = 1;
    private int _seqCliente = 1;
    private int _seqMovStock = 1;
    private int _seqCaja = 1;
    private int _seqMovCaja = 1;
    private int _seqVenta = 1;
    private int _seqDetalle = 1;
    private int _seqPago = 1;
    private int _seqComprobante = 1;

    public IReadOnlyList<MovimientoStock> MovimientosStock => _movimientosStock.AsReadOnly();
    public IReadOnlyList<CajaMovimiento> MovimientosCaja => _movimientosCaja.AsReadOnly();

    // Métodos de carga inicial para pruebas
    public Usuario AgregarUsuario(Usuario usuario)
    {
        if (usuario.Id == 0) AsignarId(usuario, nameof(Usuario.Id), _seqUsuario++);
        _usuarios.Add(usuario);
        return usuario;
    }

    public void AgregarCategoria(int id, string nombre) => _categorias[id] = nombre;

    public Producto AgregarProducto(Producto producto)
    {
        if (producto.Id == 0) producto.Id = _seqProducto++;
        _productos.Add(producto);
        return producto;
    }

    public Cliente AgregarCliente(Cliente cliente)
    {
        if (cliente.Id == 0) cliente.Id = _seqCliente++;
        _clientes.Add(cliente);
        return cliente;
    }

    public async Task EjecutarEnTransaccionAsync(Func<Task> accion)
    {
        ArgumentNullException.ThrowIfNull(accion);
        await accion();
    }

    public Task<Usuario?> ObtenerUsuarioPorIdAsync(int usuarioId)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == usuarioId));

    public Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario)
        => Task.FromResult(_usuarios.FirstOrDefault(u => string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Producto>> ListarProductosAsync()
        => Task.FromResult<IReadOnlyList<Producto>>(_productos.ToList());

    public Task<IReadOnlyDictionary<int, string>> ObtenerNombresCategoriasAsync()
        => Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>(_categorias));

    public Task<Producto?> ObtenerProductoPorIdAsync(int productoId)
        => Task.FromResult(_productos.FirstOrDefault(p => p.Id == productoId));
    
    public Task<IReadOnlyList<Cliente>> ListarClientesAsync()
        => Task.FromResult<IReadOnlyList<Cliente>>(_clientes.ToList());
    
    public Task<IReadOnlyList<Usuario>> ListarUsuariosAsync()
        => Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.ToList());

    public Task AgregarUsuarioAsync(Usuario usuario)
    {
        AgregarUsuario(usuario);
        return Task.CompletedTask;
    }

    public Task AgregarClienteAsync(Cliente cliente)
    {
        AgregarCliente(cliente);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Venta>> ListarVentasAsync()
        => Task.FromResult<IReadOnlyList<Venta>>(_ventas.ToList());

    public Task<IReadOnlyList<Pago>> ListarPagosAsync()
        => Task.FromResult<IReadOnlyList<Pago>>(_pagos.ToList());

    public Task AgregarMovimientoStockAsync(MovimientoStock movimiento)
    {
        if (movimiento.Id == 0) AsignarId(movimiento, nameof(MovimientoStock.Id), _seqMovStock++);
        _movimientosStock.Add(movimiento);
        return Task.CompletedTask;
    }

    public Task<Caja?> ObtenerCajaPorIdAsync(int cajaId)
        => Task.FromResult(_cajas.FirstOrDefault(c => c.Id == cajaId));

    public Task<Caja?> ObtenerCajaAbiertaPorUsuarioAsync(int usuarioId)
        => Task.FromResult(_cajas.FirstOrDefault(c => c.UsuarioId == usuarioId && c.Estado == EstadoCaja.Abierta));

    public Task<IReadOnlyList<CajaMovimiento>> ListarMovimientosCajaAsync(int cajaId)
        => Task.FromResult<IReadOnlyList<CajaMovimiento>>(_movimientosCaja.Where(m => m.CajaId == cajaId).ToList());

    public Task AgregarCajaAsync(Caja caja)
    {
        if (caja.Id == 0) AsignarId(caja, nameof(Caja.Id), _seqCaja++);
        _cajas.Add(caja);
        return Task.CompletedTask;
    }

    public Task AgregarMovimientoCajaAsync(CajaMovimiento movimiento)
    {
        if (movimiento.Id == 0) AsignarId(movimiento, nameof(CajaMovimiento.Id), _seqMovCaja++);
        _movimientosCaja.Add(movimiento);
        return Task.CompletedTask;
    }

    public Task<Venta?> ObtenerVentaPorIdAsync(int ventaId)
        => Task.FromResult(_ventas.FirstOrDefault(v => v.Id == ventaId));

    public Task<Venta?> ObtenerVentaPorOperacionAsync(string identificadorOperacion)
        => Task.FromResult(_ventas.FirstOrDefault(v => string.Equals(v.IdentificadorOperacion, identificadorOperacion, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Venta>> ListarBorradoresPorUsuarioAsync(int usuarioId)
        => Task.FromResult<IReadOnlyList<Venta>>(_ventas.Where(v => v.UsuarioId == usuarioId && v.Estado == EstadoVenta.Borrador).ToList());

    public Task AgregarVentaAsync(Venta venta)
    {
        if (venta.Id == 0) AsignarId(venta, nameof(Venta.Id), _seqVenta++);
        foreach (var d in venta.Detalles)
        {
            if (d.Id == 0) AsignarId(d, nameof(DetalleVenta.Id), _seqDetalle++);
            if (d.VentaId == 0) AsignarId(d, nameof(DetalleVenta.VentaId), venta.Id);
        }
        _ventas.Add(venta);
        return Task.CompletedTask;
    }

    public Task<Cliente?> ObtenerClientePorIdAsync(int clienteId)
        => Task.FromResult(_clientes.FirstOrDefault(c => c.Id == clienteId));

    public Task<Pago?> ObtenerPagoPorVentaIdAsync(int ventaId)
        => Task.FromResult(_pagos.FirstOrDefault(p => p.VentaId == ventaId));

    public Task AgregarPagoAsync(Pago pago)
    {
        if (pago.Id == 0) AsignarId(pago, nameof(Pago.Id), _seqPago++);
        _pagos.Add(pago);
        return Task.CompletedTask;
    }

    public Task<Comprobante?> ObtenerComprobantePorVentaIdAsync(int ventaId)
        => Task.FromResult(_comprobantes.FirstOrDefault(c => c.VentaId == ventaId));

    public Task<int> ObtenerSiguienteNumeroComprobanteAsync(string serie)
    {
        int max = _comprobantes
            .Where(c => string.Equals(c.Serie, serie, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Numero)
            .DefaultIfEmpty(0)
            .Max();
        return Task.FromResult(max + 1);
    }

    public Task AgregarComprobanteAsync(Comprobante comprobante)
    {
        if (comprobante.Idcomprobante == 0) AsignarId(comprobante, nameof(Comprobante.Idcomprobante), _seqComprobante++);
        _comprobantes.Add(comprobante);
        return Task.CompletedTask;
    }

    public Task<int?> ObtenerVentaIdPorOperacionAnulacionAsync(string identificadorOperacion)
        => Task.FromResult(_operacionesAnulacion.TryGetValue(identificadorOperacion, out int ventaId) ? (int?)ventaId : null);

    public Task RegistrarOperacionAnulacionAsync(string identificadorOperacion, int ventaId)
    {
        _operacionesAnulacion[identificadorOperacion] = ventaId;
        return Task.CompletedTask;
    }

    private static void AsignarId(object entidad, string nombrePropiedad, int valor)
    {
        var prop = entidad.GetType().GetProperty(nombrePropiedad, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop?.SetValue(entidad, valor);
    }
}