using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioCatalogoInventario : ICatalogoServicio, IInventarioServicio
{
    private readonly IAlmacenPos _almacen;

    public ServicioCatalogoInventario(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<ResultadoOperacion<IReadOnlyList<ProductoCatalogoDto>>> ConsultarCatalogoAsync(
        FiltroCatalogoRequest filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var productos = await _almacen.ListarProductosAsync();
        var categorias = await _almacen.ObtenerNombresCategoriasAsync();

        IEnumerable<Producto> consulta = productos;

        if (filtro.SoloActivos)
            consulta = consulta.Where(p => p.Activo);

        if (filtro.CategoriaId.HasValue)
            consulta = consulta.Where(p => p.CategoriaId == filtro.CategoriaId.Value);

        if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
        {
            string termino = filtro.TextoBusqueda.Trim();
            consulta = consulta.Where(p => p.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase));
        }

        var resultado = consulta
            .OrderBy(p => p.Nombre, StringComparer.OrdinalIgnoreCase)
            .Select(p =>
            {
                int catId = p.CategoriaId ?? 0;
                string catNombre = categorias.TryGetValue(catId, out var nombre) ? nombre : "Sin categoría";
                return new ProductoCatalogoDto(
                    p.Id,
                    p.Nombre,
                    catId,
                    catNombre,
                    p.Precio,
                    p.Stock,
                    p.Activo,
                    null);
            })
            .ToList();

        return ResultadoOperacion<IReadOnlyList<ProductoCatalogoDto>>.Ok(resultado);
    }

    public async Task<ResultadoOperacion> RegistrarMovimientoStockAsync(MovimientoStockRequest request)
    {
        if (request is null)
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "La solicitud es obligatoria.");

        if (request.Cantidad <= 0)
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "La cantidad debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "El motivo del movimiento es obligatorio.");

        if (!TryParseTipoMovimiento(request.TipoMovimiento, out var tipo))
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "Tipo de movimiento de stock inválido.");

        var usuario = await _almacen.ObtenerUsuarioPorIdAsync(request.AdminUsuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion.Fallo(CodigosErrorPos.NoAutorizado, "Usuario no válido o inactivo.");

        if (usuario.Rol != RolUsuario.Administrador)
            return ResultadoOperacion.Fallo(CodigosErrorPos.PermisoInsuficiente, "Solo un administrador puede ajustar inventario.");

        var producto = await _almacen.ObtenerProductoPorIdAsync(request.ProductoId);
        if (producto is null)
            return ResultadoOperacion.Fallo(CodigosErrorPos.NoEncontrado, "El producto indicado no existe.");

        try
        {
            await _almacen.EjecutarEnTransaccionAsync(async () =>
            {
                if (tipo == TipoMovimientoStock.EntradaProducto)
                    producto.ReponerStock(request.Cantidad);
                else
                    producto.DescontarStock(request.Cantidad);

                var movimiento = MovimientoStock.Crear(
                    producto.Id,
                    usuario.Id,
                    tipo,
                    request.Cantidad,
                    request.Motivo);

                await _almacen.AgregarMovimientoStockAsync(movimiento);
            });

            return ResultadoOperacion.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion.Fallo(CodigosErrorPos.StockInsuficiente, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, ex.Message);
        }
    }

    private static bool TryParseTipoMovimiento(string? valor, out TipoMovimientoStock tipo)
    {
        tipo = default;
        if (string.IsNullOrWhiteSpace(valor)) return false;

        switch (valor.Trim().ToLowerInvariant())
        {
            case "entrada":
            case "entradaproducto":
            case "ingreso":
            case "reposicion":
                tipo = TipoMovimientoStock.EntradaProducto;
                return true;
            case "salida":
            case "salidaproducto":
            case "egreso":
            case "merma":
                tipo = TipoMovimientoStock.SalidaProducto;
                return true;
            default:
                return false;
        }
    }
}