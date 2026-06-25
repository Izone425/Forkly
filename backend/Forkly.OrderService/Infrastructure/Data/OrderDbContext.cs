using Forkly.OrderService.Domain.Entities;
using Forkly.OrderService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Forkly.OrderService.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderSnapshot> OrderSnapshots => Set<OrderSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.OrderId);
            entity.Property(o => o.OrderId).ValueGeneratedNever(); // assigned in code
            entity.Property(o => o.UserId).IsRequired();
            entity.Property(o => o.TotalAmount).HasPrecision(12, 2);

            // Persist the enum as an uppercase string: CREATED, PAID, ...
            entity.Property(o => o.Status)
                .HasConversion(
                    v => v.ToString().ToUpperInvariant(),
                    v => Enum.Parse<OrderStatus>(v, true))
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(o => o.CreatedAt).IsRequired();

            entity.HasMany(o => o.Items)
                .WithOne(i => i.Order!)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Snapshots)
                .WithOne(s => s.Order!)
                .HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.CreatedAt);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(i => i.OrderItemId);
            entity.Property(i => i.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<OrderSnapshot>(entity =>
        {
            entity.ToTable("order_snapshots");
            entity.HasKey(s => s.SnapshotId);
            entity.Property(s => s.CreatedAt).IsRequired();
        });
    }
}
