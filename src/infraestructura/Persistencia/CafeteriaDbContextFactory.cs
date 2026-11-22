using System.Linq;
using Microsoft.EntityFrameworkCore.Design;

namespace PosCafeteria.Infraestructura.Persistencia;

public class CafeteriaDbContextFactory
    : IDesignTimeDbContextFactory<CafeteriaDbContext>
{
    public CafeteriaDbContext CreateDbContext(string[] args)
    {
        var rutaExplicita = args.FirstOrDefault(arg =>
            !string.IsNullOrWhiteSpace(arg) && !arg.StartsWith("--"));
        var cargarDemo = args.Contains("--demo");

        return ConexionSqlite.CrearContexto(rutaExplicita, cargarDemo);
    }
}