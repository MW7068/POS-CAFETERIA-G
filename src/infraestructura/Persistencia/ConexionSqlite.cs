//hecho por alex

using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class ConexionSqlite
{
    public const int SegundosEspera = 5;
    public static string RutaPredeterminada => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CafeMamaOli", "datos", "pos.db");

    internal static string ResolverRuta(string? rutaArchivo)
    {
        string? ruta = string.IsNullOrWhiteSpace(rutaArchivo)
            ? Environment.GetEnvironmentVariable("RUTA_DB")
            : rutaArchivo;

        if (!string.IsNullOrWhiteSpace(ruta))
            return ruta == ":memory:" ? ruta : Path.GetFullPath(ruta);

        string anterior = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CafeMamaOli", "POS", "cafeteria.db");
        if (!File.Exists(RutaPredeterminada) && File.Exists(anterior))
            throw new InvalidOperationException(
                "Se encontró una base en la ruta anterior. Indique esa ruta para respaldar sus datos.");

        return RutaPredeterminada;
    }

    public static CafeteriaDbContext CrearContexto(string? rutaArchivo = null, bool cargarDemo = false,
        int segundosEspera = SegundosEspera)
    {
        if (segundosEspera is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(segundosEspera));
        string ruta = ResolverRuta(rutaArchivo);
        if (ruta != ":memory:")
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        var opciones = new DbContextOptionsBuilder<CafeteriaDbContext>().UseSqlite(
            new SqliteConnectionStringBuilder
            {
                DataSource = ruta,
                ForeignKeys = true,
                Pooling = false,
                DefaultTimeout = segundosEspera
            }.ToString());
        if (cargarDemo)
        {
            opciones.UseSeeding((contexto, _) => CatalogoDemostracion.Cargar(contexto));
            opciones.UseAsyncSeeding((contexto, _, cancelacion) =>
                CatalogoDemostracion.CargarAsync(contexto, cancelacion));
        }

        var contexto = new CafeteriaDbContext(opciones.Options);
        if (ruta != ":memory:")
            return contexto;

        // El esquema de una base privada en memoria vive mientras esta conexión esté abierta.
        try
        {
            contexto.Database.OpenConnection();
            return contexto;
        }
        catch
        {
            contexto.Dispose();
            throw;
        }
    }
}
