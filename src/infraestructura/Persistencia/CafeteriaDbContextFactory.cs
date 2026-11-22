
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;

namespace PosCafeteria.Infraestructura.Persistencia;

public class CafeteriaDbContextFactory
    : IDesignTimeDbContextFactory<CafeteriaDbContext>
{
    public CafeteriaDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? ruta = null;

        int indice = Array.IndexOf(args, "--database");

        if (indice >= 0)
        {
            if (indice + 1 >= args.Length ||
                string.IsNullOrWhiteSpace(args[indice + 1]) ||
                args[indice + 1].StartsWith(
                    "--", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Indica la ruta después de --database.");
            }

            ruta = args[indice + 1];
        }

        bool cargarDemo = args.Contains("--demo");

        // ConexionSqlite resuelve RUTA_DB,
        // la ubicación anterior y la predeterminada.
        return ConexionSqlite.CrearContexto(ruta, cargarDemo);
    }
}
