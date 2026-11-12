// hecho por alex

using Microsoft.EntityFrameworkCore;
using PosCafeteria.Dominio;

namespace PosCafeteria.Infraestructura.Persistencia;

public class CafeteriaDbContext : DbContext
{
    public CafeteriaDbContext(
        DbContextOptions<CafeteriaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    // AGREGADO POR NOSOTROS:
    // Permite consultar, registrar y administrar clientes mediante EF Core.
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AGREGADO POR NOSOTROS:
        // Configuramos la nueva tabla Clientes y sus atributos.
        modelBuilder.Entity<Cliente>(entidad =>
        {
            entidad.ToTable("Clientes");

            entidad.HasKey(c => c.Id);

            entidad.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            // El nombre del cliente es obligatorio.
            entidad.Property(c => c.Nombre)
                .HasColumnType("TEXT")
                .IsRequired();

            // El celular es opcional y se almacena como texto.
            entidad.Property(c => c.Celular)
                .HasColumnType("TEXT")
                .IsRequired(false);
        });

        modelBuilder.Entity<Producto>(entidad =>
        {
            entidad.ToTable("Productos");

            entidad.HasKey(p => p.Id);

            entidad.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            entidad.Property(p => p.Nombre)
                .IsRequired();

            entidad.Property(p => p.Precio)
                .HasColumnType("TEXT")
                .IsRequired();

            entidad.Property(p => p.Stock)
                .IsRequired();
        });

        modelBuilder.Entity<Venta>(entidad =>
        {
            entidad.ToTable("Ventas");

            entidad.HasKey(v => v.Id);

            entidad.Property(v => v.Id)
                .ValueGeneratedOnAdd();

            entidad.Property(v => v.Fecha)
                .IsRequired();

            
            entidad.HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Se calcula a partir de los detalles.
            entidad.Ignore(v => v.Total);

            entidad.HasMany(v => v.Detalles)
                .WithOne()
                .HasForeignKey("VentaId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // EF trabaja con la lista interna de la entidad.
            entidad.Navigation(v => v.Detalles)
                .HasField("_detalles")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DetalleVenta>(entidad =>
        {
            entidad.ToTable("DetallesVenta");

            entidad.HasKey(d => d.Id);

            entidad.Property(d => d.Id)
                .ValueGeneratedOnAdd();

            // Columna administrada por EF, sin modificar el dominio.
            entidad.Property<int>("VentaId")
                .IsRequired();

            entidad.Property(d => d.Cantidad)
                .IsRequired();

            entidad.Property(d => d.PrecioUnitario)
                .HasColumnType("TEXT")
                .IsRequired();

            entidad.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // La entidad acumula cantidades del mismo producto.
            entidad.HasIndex(
                    "VentaId",
                    nameof(DetalleVenta.ProductoId))
                .IsUnique();
        });
    }
}