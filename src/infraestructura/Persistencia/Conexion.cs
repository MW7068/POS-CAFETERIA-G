using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace PosCafeteria.Infraestructura.Persistencia;

public static class ConexionSqlite
{
    public static CafeteriaDbContext CrearContexto(
        string rutaArchivo = "",
        bool cargarDemo = false)
    {
        // Conservado para mantener compatibles las llamadas existentes.
        _ = rutaArchivo;

        var cadenaConexion = new MySqlConnectionStringBuilder
        {
            Server = "cp54-ga.privatesystems.net",
            Port = 3306,
            Database = "apuqhawa_cafemamaoli",
            UserID = "apuqhawa_IA",
            Password = "Paco2415@@",
            SslMode = MySqlSslMode.VerifyFull,
            PersistSecurityInfo = false,
            ConnectionTimeout = 15,
            DefaultCommandTimeout = 30
        }.ToString();

        var versionServidor =
            ServerVersion.AutoDetect(cadenaConexion);

        var constructor =
            new DbContextOptionsBuilder<CafeteriaDbContext>()
                .UseMySql(cadenaConexion, versionServidor);

        if (cargarDemo)
        {
            constructor.UseSeeding(
                (contexto, _) =>
                    CatalogoDemostracion.Cargar(contexto));
        }

        return new CafeteriaDbContext(constructor.Options);
    }
}