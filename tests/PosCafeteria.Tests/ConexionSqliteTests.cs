using System.Data;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

public class ConexionSqliteTests
{
    [Fact]
    public void ContextoPuedeAbrirConexionSqliteEnMemoria()
    {
        using var contexto = ConexionSqlite.CrearContexto(":memory:");

        // Mantener la conexión abierta durante la prueba.
        contexto.Database.OpenConnection();

        var conexion = contexto.Database.GetDbConnection();

        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT 1;";

        Assert.Equal(ConnectionState.Open, conexion.State);
        Assert.Equal(1L, Assert.IsType<long>(comando.ExecuteScalar()));
    }

    [Fact]
    public void ConexionTieneActivadaLaComprobacionDeClavesForaneas()
    {
        using var contexto = ConexionSqlite.CrearContexto(":memory:");

        contexto.Database.OpenConnection();

        var conexion = contexto.Database.GetDbConnection();

        using var comando = conexion.CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys;";

        Assert.Equal(1L, Assert.IsType<long>(comando.ExecuteScalar()));
    }
}