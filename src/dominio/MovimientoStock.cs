using System;

namespace PosCafeteria.Dominio;

public class MovimientoStock
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public int UsuarioId { get; private set; }
    public int? VentaId { get; private set; }
    public TipoMovimientoStock Tipo { get; private set; }
    public int Cantidad { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;

    private MovimientoStock() { }

    public static MovimientoStock Crear(
        int productoId,
        int usuarioId,
        TipoMovimientoStock tipo,
        int cantidad,
        string motivo,
        int? ventaId = null)
    {
        if (productoId <= 0 || usuarioId <= 0)
            throw new ArgumentException("Producto o usuario inválido.");
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(cantidad));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo es obligatorio.", nameof(motivo));

        return new MovimientoStock
        {
            ProductoId = productoId,
            UsuarioId = usuarioId,
            VentaId = ventaId,
            Tipo = tipo,
            Cantidad = cantidad,
            Motivo = motivo.Trim(),
            FechaUtc = DateTime.UtcNow
        };
    }
}