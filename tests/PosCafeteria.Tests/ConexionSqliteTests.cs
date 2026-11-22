using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

[Collection("ConfiguracionSqlite")]
public class ConexionSqliteTests
{
    [Fact]
    public void ConexionEnMemoria_PermiteEjecutarSql()
    {
        using var contexto = ConexionSqlite.CrearContexto(":memory:");
        contexto.Database.OpenConnection();

        using var comando = contexto.Database
            .GetDbConnection()
            .CreateCommand();

        comando.CommandText = "SELECT 1;";

        var resultado = comando.ExecuteScalar();

        Assert.Equal(1L, resultado);
    }

    [Fact]
    public void ConexionSqlite_TieneClavesForaneasActivadas()
    {
        using var contexto = ConexionSqlite.CrearContexto(":memory:");
        contexto.Database.OpenConnection();

        using var comando = contexto.Database
            .GetDbConnection()
            .CreateCommand();

        comando.CommandText = "PRAGMA foreign_keys;";

        var resultado = comando.ExecuteScalar();

        Assert.Equal(1L, resultado);
    }

    [Fact]
    public void ResolverRutaBaseDatos_PriorizaArgumentoExplicitoSobreVariableEntorno()
    {
        var anterior = Environment.GetEnvironmentVariable("RUTA_DB");
        var carpetaTemp = Path.Combine(Path.GetTempPath(), $"pos-test-{Guid.NewGuid():N}");
        var rutaTemp = Path.Combine(carpetaTemp, "prueba.db");

        try
        {
            Environment.SetEnvironmentVariable("RUTA_DB", ":memory:");

            var resuelta = ConexionSqlite.ResolverRutaBaseDatos(rutaTemp);

            Assert.Equal(Path.GetFullPath(rutaTemp), resuelta);
            Assert.True(Directory.Exists(carpetaTemp));
        }
        finally
        {
            Environment.SetEnvironmentVariable("RUTA_DB", anterior);
            if (Directory.Exists(carpetaTemp))
            {
                Directory.Delete(carpetaTemp, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolverRutaBaseDatos_UsaVariableRutaDbEnMemoriaSinCrearCarpeta()
    {
        var anterior = Environment.GetEnvironmentVariable("RUTA_DB");

        try
        {
            Environment.SetEnvironmentVariable("RUTA_DB", ":memory:");

            var resuelta = ConexionSqlite.ResolverRutaBaseDatos();

            Assert.Equal(":memory:", resuelta);
            Assert.False(Directory.Exists(":memory:"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("RUTA_DB", anterior);
        }
    }

    [Fact]
    public void ResolverRutaBaseDatos_SinArgumentoNiVariableUsaLocalAppData()
    {
        var anterior = Environment.GetEnvironmentVariable("RUTA_DB");

        try
        {
            Environment.SetEnvironmentVariable("RUTA_DB", null);

            var resuelta = ConexionSqlite.ResolverRutaBaseDatos();
            var sufijoEsperado = Path.Combine("CafeMamaOli", "datos", "pos.db");

            Assert.EndsWith(sufijoEsperado, resuelta);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RUTA_DB", anterior);
        }
    }
}