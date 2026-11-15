using System;

namespace PosCafeteria.Dominio;

public class MovimientoStock
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public TipoMovimientoStock Tipo { get; private set; }
    public int Cantidad { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;

    private MovimientoStock() { }

    public static MovimientoStock Crear(int productoId, TipoMovimientoStock tipo, int cantidad, string motivo)
    {
        if (productoId <= 0 || cantidad == 0 || (tipo != TipoMovimientoStock.Ajuste && cantidad < 0))
            throw new ArgumentException("Datos de movimiento de stock inválidos.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo es obligatorio.", nameof(motivo));

        return new MovimientoStock
        {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            Motivo = motivo.Trim(),
            FechaUtc = DateTime.UtcNow
        };
    }
}