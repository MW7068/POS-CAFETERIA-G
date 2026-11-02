namespace PosCafeteria.Dominio;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Existencias { get; set; }

    // Propiedad de compatibilidad con pruebas de PBI-01 que usan "Stock"
    public int Stock
    {
        get => Existencias;
        set => Existencias = value;
    }

    // Constructor sin parámetros para EF Core e inicializadores de objetos
    public Producto() { }

    // Constructor parametrizado para dominio y TDD de PBI-02
    public Producto(int id, string nombre, decimal precio, int existencias = 0)
    {
        if (existencias < 0)
            throw new ArgumentException("Las existencias iniciales no pueden ser negativas.", nameof(existencias));

        Id = id;
        Nombre = nombre;
        Precio = precio;
        Existencias = existencias;
    }

    public bool TieneStockSuficiente(int cantidad) => Existencias >= cantidad;

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a descontar debe ser mayor que cero.", nameof(cantidad));

        if (!TieneStockSuficiente(cantidad))
            throw new InvalidOperationException($"Stock insuficiente para el producto '{Nombre}'. Disponible: {Existencias}, Solicitado: {cantidad}");

        Existencias -= cantidad;
    }
}