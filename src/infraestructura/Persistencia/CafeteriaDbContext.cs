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
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- ENTIDAD CLIENTE ---
        modelBuilder.Entity<Cliente>(entidad =>
        {
            entidad.ToTable("Clientes");

            entidad.HasKey(c => c.Id);

            entidad.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            entidad.Property(c => c.Nombre)
                .IsRequired();

            entidad.Property(c => c.Celular)
                .IsRequired(false);
        });

        // --- ENTIDAD PRODUCTO ---
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

        // --- ENTIDAD VENTA ---
        modelBuilder.Entity<Venta>(entidad =>
        {
            entidad.ToTable("Ventas");

            entidad.HasKey(v => v.Id);

            entidad.Property(v => v.Id)
                .ValueGeneratedOnAdd();

            entidad.Property(v => v.Fecha)
                .IsRequired();

            // Relación opcional con Cliente (RESTRICT)
            entidad.HasOne(v => v.Cliente)
                .WithMany(c => c.Ventas)
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

        // --- ENTIDAD DETALLE VENTA ---
        modelBuilder.Entity<DetalleVenta>(entidad =>
        {
            entidad.ToTable("DetallesVenta");

            entidad.HasKey(d => d.Id);

            entidad.Property(d => d.Id)
                .ValueGeneratedOnAdd();

            entidad.Property(d => d.VentaId)
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

            // La entidad acumula cantidades del mismo producto por venta.
            entidad.HasIndex(
                    nameof(DetalleVenta.VentaId),
                    nameof(DetalleVenta.ProductoId))
                .IsUnique();
        });
    }
}
