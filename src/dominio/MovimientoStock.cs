namespace PosCafeteria.Dominio;

public class MovimientoStock
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimientoStock Tipo { get; set; }
    public int Cantidad { get; set; }
    public int SaldoAnterior { get; set; }
    public int SaldoPosterior { get; set; }
    public string? ReferenciaOperacion { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public MovimientoStock() { }

    public static MovimientoStock Crear(
        Producto producto,
        TipoMovimientoStock tipo,
        int cantidadConSigno,
        int usuarioId,
        string motivo,
        string? referenciaOperacion = null)
    {
        ArgumentNullException.ThrowIfNull(producto);

        if (cantidadConSigno == 0)
            throw new ArgumentException("La cantidad del movimiento no puede ser cero.", nameof(cantidadConSigno));

        if (tipo != TipoMovimientoStock.Ajuste && cantidadConSigno < 0)
            throw new ArgumentException($"El tipo de movimiento {tipo} requiere una cantidad positiva.", nameof(cantidadConSigno));

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo del movimiento de inventario es obligatorio.", nameof(motivo));

        if (usuarioId <= 0)
            throw new ArgumentException("Se requiere un usuario válido para auditar el movimiento.", nameof(usuarioId));

        int saldoAnterior = producto.Stock;
        producto.AplicarVariacionStock(cantidadConSigno);

        return new MovimientoStock
        {
            ProductoId = producto.Id,
            Tipo = tipo,
            Cantidad = Math.Abs(cantidadConSigno),
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = producto.Stock,
            ReferenciaOperacion = referenciaOperacion,
            Motivo = motivo.Trim(),
            UsuarioId = usuarioId,
            FechaUtc = DateTime.UtcNow
        };
    }
}