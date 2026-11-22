using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class ConexionSqlite
{
    public static string ResolverRutaBaseDatos(string? rutaArchivo = null)
    {
        string rutaResuelta;

        if (!string.IsNullOrWhiteSpace(rutaArchivo))
        {
            rutaResuelta = rutaArchivo.Trim();
        }
        else
        {
            var rutaEntorno = Environment.GetEnvironmentVariable("RUTA_DB");
            if (!string.IsNullOrWhiteSpace(rutaEntorno))
            {
                rutaResuelta = rutaEntorno.Trim();
            }
            else
            {
                var localAppData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

                if (string.IsNullOrWhiteSpace(localAppData))
                {
                    localAppData = Path.GetTempPath();
                }

                rutaResuelta = Path.Combine(
                    localAppData,
                    "CafeMamaOli",
                    "datos",
                    "pos.db");
            }
        }

        if (!EsBaseEnMemoria(rutaResuelta))
        {
            var rutaCompleta = Path.GetFullPath(rutaResuelta);
            var directorio = Path.GetDirectoryName(rutaCompleta);

            if (!string.IsNullOrWhiteSpace(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            return rutaCompleta;
        }

        return ":memory:";
    }

    public static CafeteriaDbContext CrearContexto(
        string? rutaArchivo = null,
        bool cargarDemo = false)
    {
        var origenDatos = ResolverRutaBaseDatos(rutaArchivo);

        var cadenaConexion = new SqliteConnectionStringBuilder
        {
            DataSource = origenDatos,
            ForeignKeys = true
        }.ToString();

        var constructor = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(cadenaConexion);

        if (cargarDemo)
        {
            constructor.UseSeeding(
                (contexto, _) => CatalogoDemostracion.Cargar(contexto));
        }

        return new CafeteriaDbContext(constructor.Options);
    }

    private static bool EsBaseEnMemoria(string ruta)
    {
        return string.Equals(ruta.Trim(), ":memory:", StringComparison.OrdinalIgnoreCase)
            || ruta.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase);
    }
}