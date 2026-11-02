using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class CatalogoDemostracion
{
    public static void Cargar(DbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var productos = new[]
        {
            new Producto
            {
                Nombre = "Café",
                Precio = 5.00m,
                Stock = 20
            },
            new Producto
            {
                Nombre = "Sándwich",
                Precio = 8.00m,
                Stock = 15
            }
        };

        foreach (var producto in productos)
        {
            var existe = contexto.Set<Producto>()
                .Any(p => p.Nombre == producto.Nombre);

            if (!existe)
            {
                contexto.Set<Producto>().Add(producto);
            }
        }

        contexto.SaveChanges();
    }
}