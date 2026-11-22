
using System;
using System.IO;
using PosCafeteria.Infraestructura.Persistencia;
using Xunit;

namespace PosCafeteria.Tests;

public class RutaSQLiteTests
{
    [Fact]
    public void BaseAnterior_NoDebeSerIgnorada()
    {
        // Carpeta temporal exclusiva para esta prueba.
        string carpeta = Path.Combine(
            Path.GetTempPath(),
            "PosCafeteriaTests",
            Guid.NewGuid().ToString("N"));

        string anterior = Path.Combine(
            carpeta, "CafeMamaOli", "POS", "cafeteria.db");

        string nueva = Path.Combine(
            carpeta, "CafeMamaOli", "datos", "pos.db");

        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(anterior)!);

            // Simulamos una base anterior, sin datos reales.
            File.WriteAllText(anterior, "PRUEBA");

            var error = Assert.Throws<InvalidOperationException>(
                () => ConexionSqlite.ResolverRutaEnCarpeta(
                    null, null, carpeta));

            Assert.Contains("ruta anterior", error.Message);

            // No debe crear una base nueva.
            Assert.False(File.Exists(nueva));
        }
        finally
        {
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
        }
    }
}
