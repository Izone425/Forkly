namespace Forkly.MenuService.Models;

// A single menu item. The database column is "Price" (decimal RM); the REST API
// deliberately exposes it as "unitPrice". ImageUrl stores a high-resolution image
// URL only (no upload system) so the frontend can render HD cards.
public class MenuItem
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }          // exposed as "unitPrice" in the API
    public string ImageUrl { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool Availability { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
