using System;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed class ServicioClientes : IClienteServicio
{
    private readonly IAlmacenPos _almacen;

    public ServicioClientes(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<ResultadoOperacion<ClienteDto>> GestionarClienteAsync(GestionarClienteRequest request)
    {
        if (request is null)
            return ResultadoOperacion<ClienteDto>.Fallo(CodigosErrorPos.Validacion, "La solicitud es obligatoria.");

        if (!TryParseTipoDocumento(request.TipoDocumento, out var tipoDoc))
            return ResultadoOperacion<ClienteDto>.Fallo(CodigosErrorPos.Validacion, "Tipo de documento inválido (use Ninguno, DNI o RUC).");

        Cliente clienteValidado;
        try
        {
            clienteValidado = new Cliente(
                request.Nombre,
                tipoDoc,
                request.NumeroDocumento,
                request.Telefono);
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion<ClienteDto>.Fallo(CodigosErrorPos.Validacion, ex.Message);
        }

        var existentes = await _almacen.ListarClientesAsync();
        if (tipoDoc != TipoDocumentoCliente.Ninguno)
        {
            bool duplicado = existentes.Any(c =>
                c.Id != (request.ClienteId ?? 0) &&
                c.TipoDocumento == tipoDoc &&
                string.Equals(c.NumeroDocumento, clienteValidado.NumeroDocumento, StringComparison.OrdinalIgnoreCase));

            if (duplicado)
            {
                return ResultadoOperacion<ClienteDto>.Fallo(
                    CodigosErrorPos.Validacion,
                    $"Ya existe un cliente registrado con {tipoDoc} {clienteValidado.NumeroDocumento}.");
            }
        }

        Cliente clienteFinal;
        if (!request.ClienteId.HasValue || request.ClienteId.Value <= 0)
        {
            clienteFinal = clienteValidado;
            await _almacen.EjecutarEnTransaccionAsync(async () =>
            {
                await _almacen.AgregarClienteAsync(clienteFinal);
            });
        }
        else
        {
            var actual = await _almacen.ObtenerClientePorIdAsync(request.ClienteId.Value);
            if (actual is null)
                return ResultadoOperacion<ClienteDto>.Fallo(CodigosErrorPos.NoEncontrado, "El cliente indicado no existe.");

            await _almacen.EjecutarEnTransaccionAsync(() =>
            {
                actual.Nombre = clienteValidado.Nombre;
                actual.TipoDocumento = clienteValidado.TipoDocumento;
                actual.NumeroDocumento = clienteValidado.NumeroDocumento;
                actual.Telefono = clienteValidado.Telefono;
                return Task.CompletedTask;
            });

            clienteFinal = actual;
        }

        var dto = new ClienteDto(
            clienteFinal.Id,
            clienteFinal.TipoDocumento.ToString(),
            clienteFinal.NumeroDocumento,
            clienteFinal.Nombre,
            clienteFinal.Telefono,
            null,
            null);

        return ResultadoOperacion<ClienteDto>.Ok(dto);
    }

    private static bool TryParseTipoDocumento(string? valor, out TipoDocumentoCliente tipo)
    {
        tipo = default;
        if (string.IsNullOrWhiteSpace(valor))
        {
            tipo = TipoDocumentoCliente.Ninguno;
            return true;
        }

        switch (valor.Trim().ToUpperInvariant())
        {
            case "NINGUNO":
                tipo = TipoDocumentoCliente.Ninguno;
                return true;
            case "DNI":
                tipo = TipoDocumentoCliente.Dni;
                return true;
            case "RUC":
                tipo = TipoDocumentoCliente.Ruc;
                return true;
            default:
                return false;
        }
    }
}