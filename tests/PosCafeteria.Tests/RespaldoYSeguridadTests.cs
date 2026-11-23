using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;
using PosCafeteria.Infraestructura.Servicios;
using Xunit;

namespace PosCafeteria.Tests;

public class RespaldoYSeguridadTests
{
    [Fact]
    public async Task Autenticacion_PrimerAdministradorYLoginPbkdf2_BloqueaSegundoAdminYTextoPlano()
    {
        var almacen = new AlmacenPosEnMemoria();
        var authSvc = new ServicioAutenticacion(almacen);

        Assert.False(await authSvc.ExisteAdministradorAsync());

        var primerAdmin = await authSvc.RegistrarPrimerAdministradorAsync("admin", "ClaveSegura123");
        Assert.True(primerAdmin.Exitoso);
        Assert.True(await authSvc.ExisteAdministradorAsync());

        // No permite crear otro primer administrador sin autorización
        var segundoIntento = await authSvc.RegistrarPrimerAdministradorAsync("admin2", "OtraClave123");
        Assert.False(segundoIntento.Exitoso);
        Assert.Equal(CodigosErrorPos.EstadoInvalido, segundoIntento.Error!.Codigo);

        // Login correcto
        var loginOk = await authSvc.IniciarSesionAsync("admin", "ClaveSegura123");
        Assert.True(loginOk.Exitoso);
        Assert.Equal(RolUsuario.Administrador, loginOk.Valor!.Rol);

        // Login con clave incorrecta falla
        var loginFallo = await authSvc.IniciarSesionAsync("admin", "clave-mala");
        Assert.False(loginFallo.Exitoso);
        Assert.Equal(CodigosErrorPos.NoAutorizado, loginFallo.Error!.Codigo);
    }

    [Fact]
    public async Task RespaldoYRestauracion_CreaBackupValidoYRechazaArchivoCorrupto()
    {
        var almacen = new AlmacenPosEnMemoria();
        almacen.AgregarUsuario(Usuario.Crear("admin", "ClaveSegura123", RolUsuario.Administrador));

        string carpetaTemp = Path.Combine(Path.GetTempPath(), $"pos_test_backup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(carpetaTemp);

        string dbActiva = Path.Combine(carpetaTemp, "pos.db");
        string dbBackup = Path.Combine(carpetaTemp, "respaldo.db");
        string archivoCorrupto = Path.Combine(carpetaTemp, "corrupto.db");

        try
        {
            // Crear base SQLite inicial con un registro
            using (var conn = new SqliteConnection($"Data Source={dbActiva}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE EstadoPos (Valor TEXT NOT NULL); INSERT INTO EstadoPos (Valor) VALUES ('ORIGINAL');";
                cmd.ExecuteNonQuery();
                SqliteConnection.ClearPool(conn);
            }

            var respaldoSvc = new ServicioRespaldo(almacen, () => dbActiva);
            var auth = new AutorizacionAdminRequest("admin", "ClaveSegura123", "Respaldo de prueba");

            // 1. Generar respaldo
            var resBackup = await respaldoSvc.CrearRespaldoAsync(dbBackup, auth);
            Assert.True(resBackup.Exitoso);
            Assert.True(File.Exists(dbBackup));

            // 2. Modificar base activa después del respaldo
            using (var conn = new SqliteConnection($"Data Source={dbActiva}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE EstadoPos SET Valor = 'MODIFICADO';";
                cmd.ExecuteNonQuery();
                SqliteConnection.ClearPool(conn);
            }

            // 3. Rechazar restauración de archivo no SQLite
            await File.WriteAllTextAsync(archivoCorrupto, "esto no es sqlite");
            var resCorrupto = await respaldoSvc.RestaurarRespaldoAsync(archivoCorrupto, auth);
            Assert.False(resCorrupto.Exitoso);
            Assert.Equal(CodigosErrorPos.ConflictoRespaldo, resCorrupto.Error!.Codigo);

            // 4. Restaurar el respaldo válido y verificar datos originales
            var resRestore = await respaldoSvc.RestaurarRespaldoAsync(dbBackup, auth);
            Assert.True(resRestore.Exitoso);

            using (var conn = new SqliteConnection($"Data Source={dbActiva}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Valor FROM EstadoPos LIMIT 1;";
                string? valorRestaurado = cmd.ExecuteScalar()?.ToString();
                Assert.Equal("ORIGINAL", valorRestaurado);
                SqliteConnection.ClearPool(conn);
            }
        }
        finally
        {
            if (Directory.Exists(carpetaTemp))
                Directory.Delete(carpetaTemp, recursive: true);
        }
    }
}