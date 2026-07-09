using Forkly.OrderService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.OrderService.Data;

// EF Core context for the Order Service. It targets a dedicated PostgreSQL schema
// ("order") inside the shared foodorder database, with its own migrations-history
// table in that same schema, so it is fully isolated from the User service's
// public.Orders / public.OrderItems tables and migrations.
public class OrderDbContext : DbContext
{
    public const string Schema = "order";

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(o => o.Id);

            e.Property(o => o.Reference).HasMaxLength(20);
            // Unique reference. Postgres unique indexes allow multiple NULLs, so the
            // brief null-before-stamp window on insert does not collide.
            e.HasIndex(o => o.Reference).IsUnique();

            e.Property(o => o.Subtotal).HasColumnType("numeric(12,2)");
            e.Property(o => o.Sst).HasColumnType("numeric(12,2)");
            e.Property(o => o.Total).HasColumnType("numeric(12,2)");
            e.Property(o => o.Status).HasMaxLength(32).IsRequired();
            e.Property(o => o.PaymentStatus).HasMaxLength(20).IsRequired();

            // Recent / history queries filter by user and sort by CreatedAt.
            e.HasIndex(o => new { o.UserId, o.CreatedAt });

            e.HasMany(o => o.Items)
                .WithOne(i => i.Order!)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("OrderItems");
            e.HasKey(i => i.Id);

            e.Property(i => i.ItemName).HasMaxLength(200).IsRequired();
            e.Property(i => i.Price).HasColumnType("numeric(12,2)");
        });
    }
}
