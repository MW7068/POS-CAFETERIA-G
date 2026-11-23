using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioRespaldo : IRespaldoServicio
{
    private static readonly byte[] CabeceraSqlite = Encoding.ASCII.GetBytes("SQLite format 3\0");

    private readonly IAlmacenPos _almacen;
    private readonly Func<string> _obtenerRutaDbActiva;
    private readonly string _versionEsquema;

    public ServicioRespaldo(
        IAlmacenPos almacen,
        Func<string> obtenerRutaDbActiva,
        string versionEsquema = "v1-pos-sqlite")
    {
        ArgumentNullException.ThrowIfNull(almacen);
        ArgumentNullException.ThrowIfNull(obtenerRutaDbActiva);
        _almacen = almacen;
        _obtenerRutaDbActiva = obtenerRutaDbActiva;
        _versionEsquema = string.IsNullOrWhiteSpace(versionEsquema) ? "v1-pos-sqlite" : versionEsquema.Trim();
    }

    public async Task<ResultadoOperacion<ResultadoRespaldoDto>> CrearRespaldoAsync(
        string rutaDestino,
        AutorizacionAdminRequest autorizacion)
    {
        var auth = await ValidadorAutorizacionAdmin.ValidarAsync(_almacen, autorizacion);
        if (!auth.Exitoso)
            return ResultadoOperacion<ResultadoRespaldoDto>.Fallo(auth.Error!.Codigo, auth.Error.Mensaje);

        if (string.IsNullOrWhiteSpace(rutaDestino))
            return ResultadoOperacion<ResultadoRespaldoDto>.Fallo(CodigosErrorPos.Validacion, "La ruta de destino del respaldo es obligatoria.");

        string rutaActiva = _obtenerRutaDbActiva();
        if (EsRutaEnMemoria(rutaActiva))
        {
            return ResultadoOperacion<ResultadoRespaldoDto>.Fallo(
                CodigosErrorPos.ConflictoRespaldo,
                "No se puede generar respaldo desde una base de datos en memoria.");
        }

        if (!File.Exists(rutaActiva))
        {
            return ResultadoOperacion<ResultadoRespaldoDto>.Fallo(
                CodigosErrorPos.NoEncontrado,
                "No se encontró el archivo de base de datos operativo.");
        }

        string activaCompleta = Path.GetFullPath(rutaActiva);
        string destinoCompleto = Path.GetFullPath(rutaDestino.Trim());

        if (string.Equals(activaCompleta, destinoCompleto, StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoOperacion<ResultadoRespaldoDto>.Fallo(
                CodigosErrorPos.ConflictoRespaldo,
                "La ruta de respaldo no puede coincidir con la base de datos activa.");
        }

        string? directorioDestino = Path.GetDirectoryName(destinoCompleto);
        if (!string.IsNullOrWhiteSpace(directorioDestino))
            Directory.CreateDirectory(directorioDestino);

        CopiarSqliteSeguro(activaCompleta, destinoCompleto);

        var dto = new ResultadoRespaldoDto(destinoCompleto, _versionEsquema, DateTime.UtcNow);
        return ResultadoOperacion<ResultadoRespaldoDto>.Ok(dto);
    }

    public async Task<ResultadoOperacion> RestaurarRespaldoAsync(
        string rutaArchivoCandidato,
        AutorizacionAdminRequest autorizacion)
    {
        var auth = await ValidadorAutorizacionAdmin.ValidarAsync(_almacen, autorizacion);
        if (!auth.Exitoso)
            return ResultadoOperacion.Fallo(auth.Error!.Codigo, auth.Error.Mensaje);

        if (string.IsNullOrWhiteSpace(rutaArchivoCandidato))
            return ResultadoOperacion.Fallo(CodigosErrorPos.Validacion, "Debe indicar la ruta del respaldo a restaurar.");

        string candidatoCompleto = Path.GetFullPath(rutaArchivoCandidato.Trim());
        if (!File.Exists(candidatoCompleto))
            return ResultadoOperacion.Fallo(CodigosErrorPos.NoEncontrado, "El archivo de respaldo seleccionado no existe.");

        string rutaActiva = _obtenerRutaDbActiva();
        if (EsRutaEnMemoria(rutaActiva))
        {
            return ResultadoOperacion.Fallo(
                CodigosErrorPos.ConflictoRespaldo,
                "No se puede restaurar sobre una base de datos en memoria.");
        }

        string activaCompleta = Path.GetFullPath(rutaActiva);
        if (string.Equals(activaCompleta, candidatoCompleto, StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoOperacion.Fallo(
                CodigosErrorPos.ConflictoRespaldo,
                "El archivo candidato no puede ser el mismo archivo activo.");
        }

        if (!EsArchivoSqliteValido(candidatoCompleto))
        {
            return ResultadoOperacion.Fallo(
                CodigosErrorPos.ConflictoRespaldo,
                "El archivo candidato está dañado o no tiene formato SQLite válido.");
        }

        string? dirActiva = Path.GetDirectoryName(activaCompleta);
        if (!string.IsNullOrWhiteSpace(dirActiva))
            Directory.CreateDirectory(dirActiva);

        CopiarSqliteSeguro(candidatoCompleto, activaCompleta);
        return ResultadoOperacion.Ok();
    }

    private static bool EsRutaEnMemoria(string? ruta)
        => string.IsNullOrWhiteSpace(ruta) ||
           string.Equals(ruta.Trim(), ":memory:", StringComparison.OrdinalIgnoreCase);

    private static bool EsArchivoSqliteValido(string rutaArchivo)
    {
        try
        {
            byte[] buffer = new byte[16];
            using (var fs = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Read(buffer, 0, 16) < 16)
                    return false;
            }

            for (int i = 0; i < CabeceraSqlite.Length; i++)
            {
                if (buffer[i] != CabeceraSqlite[i])
                    return false;
            }

            using var conn = new SqliteConnection($"Data Source={rutaArchivo};Mode=ReadOnly");
            conn.Open();

            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.CommandText = "PRAGMA integrity_check;";
                var resultado = cmdCheck.ExecuteScalar()?.ToString();
                if (!string.Equals(resultado, "ok", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            using (var cmdTablas = conn.CreateCommand())
            {
                cmdTablas.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
                long tablas = Convert.ToInt64(cmdTablas.ExecuteScalar() ?? 0L);
                if (tablas <= 0)
                    return false;
            }

            SqliteConnection.ClearPool(conn);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CopiarSqliteSeguro(string rutaOrigen, string rutaDestino)
    {
        using var origen = new SqliteConnection($"Data Source={rutaOrigen};Mode=ReadOnly");
        using var destino = new SqliteConnection($"Data Source={rutaDestino}");
        origen.Open();
        destino.Open();
        origen.BackupDatabase(destino);
        SqliteConnection.ClearPool(destino);
        SqliteConnection.ClearPool(origen);
    }
}