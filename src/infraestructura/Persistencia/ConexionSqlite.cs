using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class ConexionSqlite
{
    public static CafeteriaDbContext CrearContexto(string rutaArchivo)
    {
        var conexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaArchivo,
            ForeignKeys = true
        }.ToString();

        var opciones = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(conexion)
            .Options;

        return new CafeteriaDbContext(opciones);
    }
}