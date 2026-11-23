using System;
using System.Linq;
using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

public sealed record UsuarioAutenticadoDto(int Id, string NombreUsuario, RolUsuario Rol);

public sealed class ServicioAutenticacion
{
    private readonly IAlmacenPos _almacen;

    public ServicioAutenticacion(IAlmacenPos almacen)
    {
        ArgumentNullException.ThrowIfNull(almacen);
        _almacen = almacen;
    }

    public async Task<bool> ExisteAdministradorAsync()
    {
        var usuarios = await _almacen.ListarUsuariosAsync();
        return usuarios.Any(u => u.Activo && u.Rol == RolUsuario.Administrador);
    }

    public async Task<ResultadoOperacion<UsuarioAutenticadoDto>> RegistrarPrimerAdministradorAsync(
        string nombreUsuario,
        string password)
    {
        if (await ExisteAdministradorAsync())
        {
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(
                CodigosErrorPos.EstadoInvalido,
                "Ya existe un administrador activo registrado en el sistema.");
        }

        try
        {
            var admin = Usuario.Crear(nombreUsuario, password, RolUsuario.Administrador);
            await _almacen.EjecutarEnTransaccionAsync(async () =>
            {
                await _almacen.AgregarUsuarioAsync(admin);
            });

            return ResultadoOperacion<UsuarioAutenticadoDto>.Ok(
                new UsuarioAutenticadoDto(admin.Id, admin.NombreUsuario, admin.Rol));
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(CodigosErrorPos.Validacion, ex.Message);
        }
    }

    public async Task<ResultadoOperacion<UsuarioAutenticadoDto>> RegistrarUsuarioAsync(
        string nombreUsuario,
        string password,
        RolUsuario rol,
        AutorizacionAdminRequest autorizacion)
    {
        var auth = await ValidadorAutorizacionAdmin.ValidarAsync(_almacen, autorizacion);
        if (!auth.Exitoso)
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(auth.Error!.Codigo, auth.Error.Mensaje);

        if (string.IsNullOrWhiteSpace(nombreUsuario))
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(CodigosErrorPos.Validacion, "El nombre de usuario es obligatorio.");

        string normalizado = nombreUsuario.Trim().ToLowerInvariant();
        var existente = await _almacen.ObtenerUsuarioPorNombreAsync(normalizado);
        if (existente is not null)
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(CodigosErrorPos.Validacion, "El nombre de usuario ya está en uso.");

        try
        {
            var nuevo = Usuario.Crear(normalizado, password, rol);
            await _almacen.EjecutarEnTransaccionAsync(async () =>
            {
                await _almacen.AgregarUsuarioAsync(nuevo);
            });

            return ResultadoOperacion<UsuarioAutenticadoDto>.Ok(
                new UsuarioAutenticadoDto(nuevo.Id, nuevo.NombreUsuario, nuevo.Rol));
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(CodigosErrorPos.Validacion, ex.Message);
        }
    }

    public async Task<ResultadoOperacion<UsuarioAutenticadoDto>> IniciarSesionAsync(
        string nombreUsuario,
        string password)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
        {
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(
                CodigosErrorPos.NoAutorizado,
                "Debe ingresar usuario y contraseña.");
        }

        var usuario = await _almacen.ObtenerUsuarioPorNombreAsync(nombreUsuario.Trim().ToLowerInvariant());
        if (usuario is null || !usuario.Activo || !usuario.VerificarPassword(password))
        {
            return ResultadoOperacion<UsuarioAutenticadoDto>.Fallo(
                CodigosErrorPos.NoAutorizado,
                "Usuario o contraseña incorrectos.");
        }

        return ResultadoOperacion<UsuarioAutenticadoDto>.Ok(
            new UsuarioAutenticadoDto(usuario.Id, usuario.NombreUsuario, usuario.Rol));
    }
}