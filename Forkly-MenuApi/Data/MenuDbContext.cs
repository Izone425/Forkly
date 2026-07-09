using Forkly.MenuService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.MenuService.Data;

// Stores its tables in the shared "public" schema of the foodorder database, alongside
// the User service's tables. To avoid clashing with the User service's migrations
// bookkeeping, the Menu service uses its own "__EFMigrationsHistoryMenu" history table
// (see Program.cs / MenuDbContextDesignFactory).
public class MenuDbContext : DbContext
{
    public const string Schema = "public";

    public MenuDbContext(DbContextOptions<MenuDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemImage> MenuItemImages => Set<MenuItemImage>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<MenuItem>(e =>
        {
            e.ToTable("MenuItems");
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(200).IsRequired();
            e.Property(m => m.Description).HasMaxLength(1000);
            e.Property(m => m.Price).HasColumnType("numeric(12,2)");
            e.Property(m => m.ImageUrl).HasMaxLength(2048);  // long enough for HD CDN URLs

            // Speed up buyer/category filtering (menu listing groups by category).
            e.HasIndex(m => new { m.CategoryId, m.Availability });

            e.HasOne(m => m.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Image)
                .WithOne(i => i.MenuItem)
                .HasForeignKey<MenuItemImage>(i => i.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Uploaded picture bytes, 1:1 with the menu item. MenuItemId doubles as PK.
        modelBuilder.Entity<MenuItemImage>(e =>
        {
            e.ToTable("MenuItemImages");
            e.HasKey(i => i.MenuItemId);
            e.Property(i => i.ContentType).HasMaxLength(64);
        });

        // Short-lived per-session stock holds (cart reservations). One row per
        // (item, session); the unique index makes that an upsertable key. Extra indexes
        // speed the availability sum (by item) and expiry sweeps (by ExpiresAt).
        modelBuilder.Entity<StockReservation>(e =>
        {
            e.ToTable("StockReservations");
            e.HasKey(r => r.Id);
            e.Property(r => r.SessionId).HasMaxLength(64).IsRequired();
            e.HasIndex(r => new { r.MenuItemId, r.SessionId }).IsUnique();
            e.HasIndex(r => r.MenuItemId);
            e.HasIndex(r => r.ExpiresAt);

            e.HasOne(r => r.MenuItem)
                .WithMany()
                .HasForeignKey(r => r.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
