
using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

public class MigracionHistoricaTests
{
    [Fact]
    public void ActualizarDesdeMigracionAnterior_ConservaYProtegeDatosHistoricos()
    {
        // Base independiente, solo para esta prueba.
        using var conexion = new SqliteConnection(
            "Data Source=:memory:;Foreign Keys=True");

        conexion.Open();

        var opciones = new DbContextOptionsBuilder<CafeteriaDbContext>()
            .UseSqlite(conexion)
            .Options;

        // PASO 1: Aplicar solamente la migracion anterior.
        using (var contexto = new CafeteriaDbContext(opciones))
        {
            contexto.Database.Migrate(
                "20261102045256_InicialPersistencia");
        }

        // PASO 2: Insertar datos que simulan una venta antigua.
        Ejecutar(conexion, """
            INSERT INTO "Productos"
                ("Id", "Nombre", "Precio", "Stock")
            VALUES
                (1001, 'Cafe historico', '7.50', 12);
            """);

        Ejecutar(conexion, """
            INSERT INTO "Ventas"
                ("Id", "Fecha")
            VALUES
                (2001, '2026-10-30 15:45:00');
            """);

        Ejecutar(conexion, """
            INSERT INTO "DetallesVenta"
                ("Id", "ProductoId", "Cantidad",
                 "PrecioUnitario", "VentaId")
            VALUES
                (3001, 1001, 2, '7.50', 2001);
            """);

        // Verificar que existian antes de actualizar.
        Assert.Equal(1L, Numero(conexion,
            """SELECT COUNT(*) FROM "Productos";"""));

        Assert.Equal(1L, Numero(conexion,
            """SELECT COUNT(*) FROM "Ventas";"""));

        Assert.Equal(1L, Numero(conexion,
            """SELECT COUNT(*) FROM "DetallesVenta";"""));

        // PASO 3: Ejecutar las migraciones posteriores.
        // Incluye ModeloCompletoPbi03 y ProtegerVentasHistoricas.
        using (var contexto = new CafeteriaDbContext(opciones))
        {
            contexto.Database.Migrate();
        }

        // PASO 4: Comprobar conservacion de datos.
        Assert.Equal("Cafe historico", Texto(conexion, """
            SELECT "Nombre"
            FROM "Productos"
            WHERE "Id" = 1001;
            """));

        Assert.Equal(12L, Numero(conexion, """
            SELECT "Stock"
            FROM "Productos"
            WHERE "Id" = 1001;
            """));

        Assert.Equal("7.50", Texto(conexion, """
            SELECT "PrecioUnitario"
            FROM "DetallesVenta"
            WHERE "Id" = 3001;
            """));

        Assert.Equal(2L, Numero(conexion, """
            SELECT "Cantidad"
            FROM "DetallesVenta"
            WHERE "Id" = 3001;
            """));

        Assert.Equal("Cafe historico", Texto(conexion, """
            SELECT "NombreProducto"
            FROM "DetallesVenta"
            WHERE "Id" = 3001;
            """));

        Assert.Equal("2026-10-30 15:45:00",
            Texto(conexion, """
                SELECT "Fecha"
                FROM "Ventas"
                WHERE "Id" = 2001;
                """));

        // PASO 5: Verificar estado historico desconocido.
        Assert.Equal(0L, Numero(conexion, """
            SELECT "Estado"
            FROM "Ventas"
            WHERE "Id" = 2001;
            """));

        Assert.Equal(1L, Numero(conexion, """
            SELECT "EsHistorica"
            FROM "Ventas"
            WHERE "Id" = 2001;
            """));

        // El cliente era desconocido: debe seguir siendo NULL.
        Assert.Equal(1L, Numero(conexion, """
            SELECT COUNT(*)
            FROM "Ventas"
            WHERE "Id" = 2001
              AND "ClienteId" IS NULL;
            """));

        // PASO 6: Verificar el usuario tecnico desactivado.
        Assert.Equal(1L, Numero(conexion, """
            SELECT COUNT(*)
            FROM "Ventas" v
            INNER JOIN "Usuarios" u
                ON v."UsuarioId" = u."Id"
            WHERE v."Id" = 2001
              AND u."NombreUsuario" = '__ventas_historicas__'
              AND u."Activo" = 0;
            """));

        // PASO 7: Verificar proteccion en el dominio.
        using (var contexto = new CafeteriaDbContext(opciones))
        {
            var venta = contexto.Ventas.Single(v => v.Id == 2001);

            Assert.NotEqual(EstadoVenta.Borrador, venta.Estado);

            Assert.True(
                contexto.Entry(venta)
                    .Property<bool>("EsHistorica")
                    .CurrentValue);

            Assert.Throws<InvalidOperationException>(
                () => venta.AsignarCliente(null));
        }

        // PASO 8: Verificar proteccion directa en SQLite.
        var errorActualizar = Assert.Throws<SqliteException>(
            () => Ejecutar(conexion, """
                UPDATE "Ventas"
                SET "Estado" = 1
                WHERE "Id" = 2001;
                """));

        Assert.Equal(19, errorActualizar.SqliteErrorCode);

        var errorEliminar = Assert.Throws<SqliteException>(
            () => Ejecutar(conexion, """
                DELETE FROM "Ventas"
                WHERE "Id" = 2001;
                """));

        Assert.Equal(19, errorEliminar.SqliteErrorCode);

        var errorDetalle = Assert.Throws<SqliteException>(
            () => Ejecutar(conexion, """
                UPDATE "DetallesVenta"
                SET "Cantidad" = 5
                WHERE "Id" = 3001;
                """));

        Assert.Equal(19, errorDetalle.SqliteErrorCode);

        Assert.Throws<SqliteException>(
            () => Ejecutar(conexion, """
                DELETE FROM "DetallesVenta"
                WHERE "Id" = 3001;
                """));

        Assert.Throws<SqliteException>(
            () => Ejecutar(conexion, """
                INSERT INTO "DetallesVenta"
                    ("Id", "ProductoId", "VentaId",
                     "Cantidad", "PrecioUnitario",
                     "NombreProducto")
                VALUES
                    (3002, 1001, 2001,
                     1, '7.50', 'Cafe historico');
                """));

        // Los registros siguen intactos.
        Assert.Equal(1L, Numero(conexion, """
            SELECT COUNT(*)
            FROM "Ventas"
            WHERE "Id" = 2001 AND "Estado" = 0;
            """));

        Assert.Equal(2L, Numero(conexion, """
            SELECT "Cantidad"
            FROM "DetallesVenta"
            WHERE "Id" = 3001;
            """));

        // PASO 9: Las ventas nuevas deben poder modificarse.
        Ejecutar(conexion, """
            INSERT INTO "Usuarios"
                ("Id", "NombreUsuario", "Password",
                 "Rol", "Activo")
            VALUES
                (4001, 'cajero_prueba_migracion',
                 'SoloDatoDePrueba', 2, 1);
            """);

        
        Ejecutar(conexion, """
            INSERT INTO "Ventas"
                ("Id", "Fecha", "UsuarioId",
                "Estado", "Modalidad", "Descuento")
            VALUES
                (2002, '2026-11-25 11:00:00',
                4001, 1, 1, '0.00');
            """);


        Ejecutar(conexion, """
            UPDATE "Ventas"
            SET "Modalidad" = 2
            WHERE "Id" = 2002;
            """);

        Assert.Equal(0L, Numero(conexion, """
            SELECT "EsHistorica"
            FROM "Ventas"
            WHERE "Id" = 2002;
            """));

        Assert.Equal(2L, Numero(conexion, """
            SELECT "Modalidad"
            FROM "Ventas"
            WHERE "Id" = 2002;
            """));

        // PASO 10: Comprobar las claves foraneas.
        Assert.Equal(1L, Numero(conexion,
            "PRAGMA foreign_keys;"));

        var errorFk = Assert.Throws<SqliteException>(
            () => Ejecutar(conexion, """
                INSERT INTO "DetallesVenta"
                    ("Id", "ProductoId", "VentaId",
                     "Cantidad", "PrecioUnitario",
                     "NombreProducto")
                VALUES
                    (3003, 999999, 2002,
                     1, '7.50', 'Inexistente');
                """));

        Assert.Equal(787, errorFk.SqliteExtendedErrorCode);

        // Ninguna clave foranea debe quedar inconsistente.
        Assert.Null(Valor(conexion,
            "PRAGMA foreign_key_check;"));

        // PASO 11: Verificar integridad general de SQLite.
        Assert.Equal("ok", Texto(conexion,
            "PRAGMA integrity_check;"));
    }

    private static void Ejecutar(
        SqliteConnection conexion,
        string sql)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }

    private static object? Valor(
        SqliteConnection conexion,
        string sql)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        return comando.ExecuteScalar();
    }

    private static long Numero(
        SqliteConnection conexion,
        string sql)
    {
        return Convert.ToInt64(Valor(conexion, sql));
    }

    private static string? Texto(
        SqliteConnection conexion,
        string sql)
    {
        return Convert.ToString(Valor(conexion, sql));
    }
}
