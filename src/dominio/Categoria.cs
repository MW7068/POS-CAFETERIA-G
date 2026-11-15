namespace PosCafeteria.Dominio;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;

    public Categoria() { }

    public Categoria(string nombre)
    {
        ActualizarNombre(nombre);
        Activa = true;
    }

    public void ActualizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría es obligatorio.", nameof(nombre));

        Nombre = nombre.Trim();
    }

    public void Activar() => Activa = true;
    public void Desactivar() => Activa = false;
}