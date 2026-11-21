// hecho por alex   


using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;

namespace PosCafeteria.Infraestructura.Persistencia;

public class CafeteriaDbContextFactory
    : IDesignTimeDbContextFactory<CafeteriaDbContext>
{
    public CafeteriaDbContext CreateDbContext(string[] args)
    {
        string? ruta = Environment.GetEnvironmentVariable("RUTA_DB");

        int indice = Array.IndexOf(args, "--database");
        if (indice >= 0)
        {
            if (indice + 1 >= args.Length ||
                string.IsNullOrWhiteSpace(args[indice + 1]) ||
                args[indice + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Indica la ruta después de --database.");
            }

            ruta = args[indice + 1];
        }

        if (string.IsNullOrWhiteSpace(ruta))
        {
            ruta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CafeMamaOli",
                "datos",
                "pos.db");
        }

        if (ruta != ":memory:")
        {
            ruta = Path.GetFullPath(ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        }

        bool cargarDemo = args.Contains("--demo");
        return ConexionSqlite.CrearContexto(ruta, cargarDemo);
    }
}