using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;

namespace PosCafeteria.Infraestructura.Persistencia;

public class CafeteriaDbContextFactory
    : IDesignTimeDbContextFactory<CafeteriaDbContext>
{
    public CafeteriaDbContext CreateDbContext(string[] args)
    {
        var ruta = Path.GetFullPath("cafeteria.desarrollo.db");
        var cargarDemo = args.Contains("--demo");

        return ConexionSqlite.CrearContexto(ruta, cargarDemo);
    }
}