using System;

namespace PosCafeteria.Dominio;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int? CategoriaId { get; set; }
    public bool Activo { get; set; } = true;

    public Producto() { }

    public Producto(string nombre, decimal precio, int stock = 0, int? categoriaId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (precio < 0m || stock < 0)
            throw new ArgumentException("Precio o stock inválido.");

        Nombre = nombre.Trim();
        Precio = decimal.Round(precio, 2, MidpointRounding.AwayFromZero);
        Stock = stock;
        CategoriaId = categoriaId;
    }

    public void EstablecerStock(int nuevoStock)
    {
        if (nuevoStock < 0)
            throw new InvalidOperationException("El stock no puede ser negativo.");
        Stock = nuevoStock;
    }
}