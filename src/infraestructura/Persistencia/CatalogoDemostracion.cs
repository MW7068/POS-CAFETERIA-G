// hecho por alex

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class CatalogoDemostracion
{
    // Las existencias se incorporan mediante un movimiento de stock identificable.
    private static Producto[] ProductosIniciales() =>
        [
            new Producto
            {
                Nombre = "Café",
                Precio = 5.00m,
                Stock = 0
            },
            new Producto
            {
                Nombre = "Sándwich",
                Precio = 8.00m,
                Stock = 0
            }
        ];

    public static void Cargar(DbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var productos = contexto.Set<Producto>();
        foreach (var producto in ProductosIniciales())
        {
            if (!productos.Any(p => p.Nombre == producto.Nombre))
                productos.Add(producto);
        }

        contexto.SaveChanges();
    }

    public static async Task CargarAsync(DbContext contexto, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var productos = contexto.Set<Producto>();
        foreach (var producto in ProductosIniciales())
        {
            if (!await productos.AnyAsync(p => p.Nombre == producto.Nombre, cancelacion))
                productos.Add(producto);
        }
        await contexto.SaveChangesAsync(cancelacion);
    }
}
