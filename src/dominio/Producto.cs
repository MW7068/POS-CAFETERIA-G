using System;

namespace PosCafeteria.Dominio;

public class Producto
{
    private decimal _precio;

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio
    {
        get => _precio;
        set
        {
            ValidarPrecio(value);
            _precio = value;
        }
    }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public int? CategoriaId { get; set; }
    public bool Activo { get; set; } = true;

    public bool EstaBajoStockMinimo => Stock <= StockMinimo;

    public Producto() { }

    public Producto(string nombre, decimal precio, int stock = 0, int stockMinimo = 0, int? categoriaId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(nombre));
        if (stock < 0 || stockMinimo < 0)
            throw new ArgumentException("Stock o stock mínimo inválido.");

        ValidarPrecio(precio);

        Nombre = nombre.Trim();
        _precio = precio;
        Stock = stock;
        StockMinimo = stockMinimo;
        CategoriaId = categoriaId;
    }

    private static void ValidarPrecio(decimal precio)
    {
        if (precio <= 0m)
            throw new ArgumentException("El precio del producto debe ser mayor que cero.", nameof(precio));
        if (decimal.Round(precio, 2, MidpointRounding.AwayFromZero) != precio)
            throw new ArgumentException("El precio del producto no puede tener más de dos decimales.", nameof(precio));
    }

    public void EstablecerStock(int nuevoStock)
    {
        if (nuevoStock < 0)
            throw new InvalidOperationException("El stock no puede ser negativo.");
        Stock = nuevoStock;
    }

    public void ReponerStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a reponer debe ser mayor a cero.", nameof(cantidad));
        Stock = checked(Stock + cantidad);
    }

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a descontar debe ser mayor a cero.", nameof(cantidad));
        if (Stock < cantidad)
            throw new InvalidOperationException($"Stock insuficiente para '{Nombre}'.");
        Stock -= cantidad;
    }
}