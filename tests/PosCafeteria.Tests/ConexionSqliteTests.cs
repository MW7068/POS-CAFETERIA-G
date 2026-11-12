using Microsoft.EntityFrameworkCore;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

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
}
