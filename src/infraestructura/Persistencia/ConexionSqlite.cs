using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class ConexionSqlite
{
    public static CafeteriaDbContext CrearContexto(
        string rutaArchivo,
        bool cargarDemo = false)
    {
        var cadenaConexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaArchivo,
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
}
