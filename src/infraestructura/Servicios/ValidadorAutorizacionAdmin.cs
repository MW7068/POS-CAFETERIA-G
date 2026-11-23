using System.Threading.Tasks;
using PosCafeteria.Dominio;
using PosCafeteria.Dominio.Contratos;

namespace PosCafeteria.Infraestructura.Servicios;

internal static class ValidadorAutorizacionAdmin
{
    public static async Task<ResultadoOperacion<Usuario>> ValidarAsync(
        IAlmacenPos almacen,
        AutorizacionAdminRequest? autorizacion)
    {
        if (autorizacion is null)
        {
            return ResultadoOperacion<Usuario>.Fallo(
                CodigosErrorPos.NoAutorizado,
                "Se requiere autorización de un administrador para continuar.");
        }

        if (string.IsNullOrWhiteSpace(autorizacion.UsuarioAdmin) ||
            string.IsNullOrWhiteSpace(autorizacion.ClaveAdmin))
        {
            return ResultadoOperacion<Usuario>.Fallo(
                CodigosErrorPos.NoAutorizado,
                "Debe ingresar el usuario y la contraseña del administrador.");
        }

        if (string.IsNullOrWhiteSpace(autorizacion.Motivo))
        {
            return ResultadoOperacion<Usuario>.Fallo(
                CodigosErrorPos.Validacion,
                "Debe indicar el motivo de la autorización administrativa.");
        }

        var admin = await almacen.ObtenerUsuarioPorNombreAsync(autorizacion.UsuarioAdmin.Trim().ToLowerInvariant());
        if (admin is null || !admin.Activo || !admin.VerificarPassword(autorizacion.ClaveAdmin))
        {
            return ResultadoOperacion<Usuario>.Fallo(
                CodigosErrorPos.NoAutorizado,
                "Credenciales de administrador inválidas o usuario inactivo.");
        }

        if (admin.Rol != RolUsuario.Administrador)
        {
            return ResultadoOperacion<Usuario>.Fallo(
                CodigosErrorPos.PermisoInsuficiente,
                "El usuario indicado no tiene rol de Administrador.");
        }

        return ResultadoOperacion<Usuario>.Ok(admin);
    }
}