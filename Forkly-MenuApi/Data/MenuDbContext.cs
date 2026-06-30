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
        });
    }
}
