using Microsoft.EntityFrameworkCore;

namespace PosCafeteria.Infraestructura.Persistencia;

public class CafeteriaDbContext : DbContext
{
    public CafeteriaDbContext(
        DbContextOptions<CafeteriaDbContext> options)
        : base(options)
    {
    }
}
