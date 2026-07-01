using System.ComponentModel.DataAnnotations.Schema;

namespace Forkly.MenuService.Models;

// A single menu item. The database column is "Price" (decimal RM); the REST API
// deliberately exposes it as "unitPrice". Uploaded pictures are stored as bytes in
// the 1:1 MenuItemImage table; the legacy ImageUrl column holds any external CDN URL
// (e.g. seeded Unsplash images) and is used as a fallback when no upload exists.
public class MenuItem
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }          // exposed as "unitPrice" in the API
    public string ImageUrl { get; set; } = string.Empty;   // legacy/external URL fallback
    public int StockQuantity { get; set; }
    public bool Availability { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Uploaded picture bytes (1:1). Never Include()d in listings — the image bytes are
    // served only by the dedicated image endpoint.
    public MenuItemImage? Image { get; set; }

    // Populated by the repository from a byte-free metadata query so the response can
    // resolve the served image URL without ever loading the image bytes in a listing.
    [NotMapped] public bool HasImage { get; set; }
    [NotMapped] public DateTimeOffset? ImageUpdatedAt { get; set; }
}
