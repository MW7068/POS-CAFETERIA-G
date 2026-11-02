using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PosCafeteria.Tests")]

namespace PosCafeteria.Dominio;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Existencias { get; set; }

    public int Stock
    {
        get => Existencias;
        set => Existencias = value;
    }

    // Constructor restringido para EF Core y accesible para el proyecto de pruebas
    internal Producto() { }

    public Producto(int id, string nombre, decimal precio, int existencias = 0)
    {
        if (precio <= 0)
            throw new ArgumentException("El precio debe ser mayor que cero.", nameof(precio));

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