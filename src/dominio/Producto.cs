namespace PosCafeteria.Dominio;

public class Producto
{
    public int Id { get; set; }
    public int? CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? Icono { get; set; }
    public bool Activo { get; set; } = true;

    // Constructor sin parámetros requerido por EF Core y compatibilidad con pruebas existentes
    public Producto() { }

    // Constructor de dominio para altas en catálogo (PBI-03): inicia siempre con stock 0
    public Producto(string nombre, decimal precio, int? categoriaId = null, string? icono = null)
    {
        ActualizarDatos(nombre, precio, categoriaId, icono);
        Stock = 0;
        Activo = true;
    }

    public void ActualizarDatos(string nombre, decimal precio, int? categoriaId = null, string? icono = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto no puede estar vacío.", nameof(nombre));

        ValidarPrecio(precio);

        Nombre = nombre.Trim();
        Precio = precio;
        CategoriaId = categoriaId;
        Icono = string.IsNullOrWhiteSpace(icono) ? null : icono.Trim();
    }

    public bool TieneStockSuficiente(int cantidad) => cantidad > 0 && Stock >= cantidad;

    public void AplicarVariacionStock(int delta)
    {
        long nuevoSaldo = (long)Stock + delta;
        if (nuevoSaldo < 0)
            throw new InvalidOperationException($"Stock insuficiente para '{Nombre}'. Disponible: {Stock}.");
        if (nuevoSaldo > int.MaxValue)
            throw new OverflowException("El stock supera el límite numérico permitido.");

        Stock = (int)nuevoSaldo;
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;

    public static void ValidarPrecio(decimal precio)
    {
        if (precio <= 0)
            throw new ArgumentException("El precio debe ser mayor que cero.", nameof(precio));

        if (decimal.Round(precio, 2, MidpointRounding.AwayFromZero) != precio)
            throw new ArgumentException("El precio no puede tener más de dos decimales.", nameof(precio));
    }
}