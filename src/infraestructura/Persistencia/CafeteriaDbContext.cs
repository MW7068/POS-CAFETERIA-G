
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

    // Entidades del dominio
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<CajaMovimiento> MovimientosCaja => Set<CajaMovimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CATEGORIAS
        modelBuilder.Entity<Categoria>(entidad =>
        {
            entidad.ToTable("Categorias");
            entidad.HasKey(c => c.Id);

            entidad.Property(c => c.Id).ValueGeneratedOnAdd();
            entidad.Property(c => c.Nombre).IsRequired();
            entidad.Property(c => c.Activa).IsRequired();
        });

        // PRODUCTOS
        modelBuilder.Entity<Producto>(entidad =>
        {
            entidad.ToTable("Productos");
            entidad.HasKey(p => p.Id);

            entidad.Property(p => p.Id).ValueGeneratedOnAdd();
            entidad.Property(p => p.Nombre).IsRequired();

            entidad.Property(p => p.Precio)
                .HasColumnType("TEXT")
                .IsRequired();

            entidad.Property(p => p.Stock).IsRequired();

            entidad.Property(p => p.StockMinimo)
                .HasDefaultValue(0);

            entidad.Property(p => p.Activo)
                .HasDefaultValue(true);

            entidad.Ignore(p => p.EstaBajoStockMinimo);

            entidad.HasOne<Categoria>()
                .WithMany()
                .HasForeignKey(p => p.CategoriaId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CLIENTES
        modelBuilder.Entity<Cliente>(entidad =>
        {
            entidad.ToTable("Clientes");
            entidad.HasKey(c => c.Id);

            entidad.Property(c => c.Id).ValueGeneratedOnAdd();
            entidad.Property(c => c.Nombre).IsRequired();

            entidad.Property(c => c.TipoDocumento)
                .HasConversion<int>();

            entidad.Property(c => c.NumeroDocumento)
                .IsRequired();

            entidad.Property(c => c.Telefono)
                .IsRequired(false);

            // Los documentos vacios no deben bloquear
            // la existencia de varios clientes sin documento.
            entidad.HasIndex(c => new
                {
                    c.TipoDocumento,
                    c.NumeroDocumento
                })
                .IsUnique()
                .HasFilter("\"NumeroDocumento\" <> ''");
        });

        // USUARIOS
        modelBuilder.Entity<Usuario>(entidad =>
        {
            entidad.ToTable("Usuarios");
            entidad.HasKey(u => u.Id);

            entidad.Property(u => u.Id).ValueGeneratedOnAdd();

            entidad.Property(u => u.NombreUsuario)
                .IsRequired();

            entidad.Property(u => u.Password)
                .IsRequired();

            entidad.Property(u => u.Rol)
                .HasConversion<int>();

            entidad.Property(u => u.Activo)
                .IsRequired();

            entidad.HasIndex(u => u.NombreUsuario)
                .IsUnique();
        });

        // CAJAS
        modelBuilder.Entity<Caja>(entidad =>
        {
            entidad.ToTable("Cajas");
            entidad.HasKey(c => c.Id);

            entidad.Property(c => c.Id).ValueGeneratedOnAdd();

            entidad.Property(c => c.MontoInicial)
                .HasColumnType("TEXT");

            entidad.Property(c => c.MontoContado)
                .HasColumnType("TEXT");

            entidad.Property(c => c.TotalIngresosEfectivo)
                .HasColumnType("TEXT");

            entidad.Property(c => c.TotalEgresosEfectivo)
                .HasColumnType("TEXT");

            entidad.Property(c => c.TotalEsperado)
                .HasColumnType("TEXT");

            entidad.Property(c => c.Diferencia)
                .HasColumnType("TEXT");

            entidad.Property(c => c.Estado)
                .HasConversion<int>();

            entidad.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // VENTAS
        modelBuilder.Entity<Venta>(entidad =>
        {
            entidad.ToTable("Ventas");
            entidad.HasKey(v => v.Id);

            entidad.Property(v => v.Id).ValueGeneratedOnAdd();
            entidad.Property(v => v.Fecha).IsRequired();

            entidad.Property(v => v.Estado)
                .HasConversion<int>();

            entidad.Property(v => v.Modalidad)
                .HasConversion<int>();

            entidad.Property(v => v.Descuento)
                .HasColumnType("TEXT");

            // Valores calculados por el dominio.
            entidad.Ignore(v => v.Total);
            entidad.Ignore(v => v.SubtotalBruto);

            // Relacion obligatoria con usuario.
            entidad.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(v => v.UsuarioId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Una venta puede no tener cliente.
            entidad.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Las ventas en borrador pueden no tener caja.
            entidad.HasOne<Caja>()
                .WithMany()
                .HasForeignKey(v => v.CajaId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Evita duplicar operaciones no nulas.
            entidad.HasIndex(v => v.IdentificadorOperacion)
                .IsUnique();

            // Conservamos la lista privada de detalles.
            entidad.HasMany(v => v.Detalles)
                .WithOne()
                .HasForeignKey(d => d.VentaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entidad.Navigation(v => v.Detalles)
                .HasField("_detalles")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // DETALLES DE VENTA
        modelBuilder.Entity<DetalleVenta>(entidad =>
        {
            entidad.ToTable("DetallesVenta");
            entidad.HasKey(d => d.Id);

            entidad.Property(d => d.Id).ValueGeneratedOnAdd();

            entidad.Property(d => d.VentaId)
                .IsRequired();

            entidad.Property(d => d.NombreProducto)
                .IsRequired();

            entidad.Property(d => d.Cantidad)
                .IsRequired();

            entidad.Property(d => d.PrecioUnitario)
                .HasColumnType("TEXT")
                .IsRequired();

            entidad.Property(d => d.Observacion)
                .IsRequired(false);

            entidad.Ignore(d => d.Subtotal);

            entidad.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Se permiten observaciones distintas
            // para el mismo producto dentro de una venta.
            entidad.HasIndex(d => new
            {
                d.VentaId,
                d.ProductoId
            });
        });

        // MOVIMIENTOS DE STOCK
        modelBuilder.Entity<MovimientoStock>(entidad =>
        {
            entidad.ToTable("MovimientosStock");
            entidad.HasKey(m => m.Id);

            entidad.Property(m => m.Id).ValueGeneratedOnAdd();
            entidad.Property(m => m.Tipo).HasConversion<int>();

            entidad.Property(m => m.Motivo).IsRequired();
            entidad.Property(m => m.FechaUtc).IsRequired();

            entidad.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne<Venta>()
                .WithMany()
                .HasForeignKey(m => m.VentaId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PAGOS
        modelBuilder.Entity<Pago>(entidad =>
        {
            entidad.ToTable("Pagos");
            entidad.HasKey(p => p.Id);

            entidad.Property(p => p.Id).ValueGeneratedOnAdd();
            entidad.Property(p => p.Metodo).HasConversion<int>();

            entidad.Property(p => p.MontoConsumo).HasColumnType("TEXT");
            entidad.Property(p => p.MontoPropina).HasColumnType("TEXT");
            entidad.Property(p => p.MontoTotal).HasColumnType("TEXT");
            entidad.Property(p => p.MontoRecibido).HasColumnType("TEXT");

            entidad.Ignore(p => p.Vuelto);

            entidad.HasOne<Venta>()
                .WithMany()
                .HasForeignKey(p => p.VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne<Caja>()
                .WithMany()
                .HasForeignKey(p => p.CajaId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasIndex(p => p.VentaId)
                .IsUnique();
        });

        // COMPROBANTES INTERNOS
        modelBuilder.Entity<Comprobante>(entidad =>
        {
            entidad.ToTable("Comprobantes");
            entidad.HasKey(c => c.Idcomprobante);

            entidad.Property(c => c.Idcomprobante)
                .ValueGeneratedOnAdd();

            entidad.Property(c => c.Serie).IsRequired();
            entidad.Property(c => c.ClienteNombre).IsRequired();

            entidad.Property(c => c.Total)
                .HasColumnType("TEXT");

            entidad.Ignore(c => c.NumeroFormateado);

            entidad.HasOne<Venta>()
                .WithMany()
                .HasForeignKey(c => c.VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasIndex(c => c.VentaId)
                .IsUnique();

            entidad.HasIndex(c => new
            {
                c.Serie,
                c.Numero
            }).IsUnique();
        });

        // MOVIMIENTOS DE CAJA
        modelBuilder.Entity<CajaMovimiento>(entidad =>
        {
            entidad.ToTable("CajaMovimientos");
            entidad.HasKey(m => m.Id);

            entidad.Property(m => m.Id).ValueGeneratedOnAdd();

            entidad.Property(m => m.Tipo)
                .HasConversion<int>();

            entidad.Property(m => m.Origen)
                .HasConversion<int>();

            entidad.Property(m => m.MetodoPago)
                .HasConversion<int>();

            entidad.Property(m => m.Concepto).IsRequired();

            entidad.Property(m => m.Monto)
                .HasColumnType("TEXT");

            entidad.HasOne<Caja>()
                .WithMany()
                .HasForeignKey(m => m.CajaId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne<Venta>()
                .WithMany()
                .HasForeignKey(m => m.VentaId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CONFIGURACION DEL NEGOCIO
        modelBuilder.Entity<Negocio>(entidad =>
        {
            entidad.ToTable("Negocios");
            entidad.HasKey(n => n.Id);

            entidad.Property(n => n.Id).ValueGeneratedOnAdd();

            entidad.Property(n => n.NombreNegocio)
                .IsRequired();

            entidad.Property(n => n.TasaImpuesto)
                .HasColumnType("TEXT");
        });
    }
}
