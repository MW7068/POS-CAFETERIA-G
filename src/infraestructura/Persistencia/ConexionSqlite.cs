using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class ConexionSqlite
{
    public static CafeteriaDbContext CrearContexto(string rutaArchivo)
    {
        var cadenaConexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaArchivo,
            ForeignKeys = true
        }.ToString();

        var opciones = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(cadenaConexion)
            .Options;

        return new CafeteriaDbContext(opciones);
    }
}