using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;
using PosCafeteria.Infraestructura.Persistencia;

namespace PosCafeteria.Infraestructura.Repositorios;

public class RepositorioVentas
{
    private readonly CafeteriaDbContext _contexto;

    public RepositorioVentas(CafeteriaDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    public int Guardar(Venta venta)
    {
        ArgumentNullException.ThrowIfNull(venta);

        if (venta.Id != 0)
        {
            throw new InvalidOperationException(
                "La venta ya tiene un identificador.");
        }

        if (venta.Detalles.Count == 0)
        {
            throw new InvalidOperationException(
                "No se puede guardar una venta sin detalles.");
        }

        _contexto.Ventas.Add(venta);
        _contexto.SaveChanges();

        return venta.Id;
    }

    public Venta? ObtenerPorId(int id)
    {
        return _contexto.Ventas
            .AsNoTracking()
            .Include(v => v.Detalles)
            .SingleOrDefault(v => v.Id == id);
    }
}