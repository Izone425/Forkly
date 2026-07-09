using System.ComponentModel.DataAnnotations;

namespace Forkly.MenuService.Dtos;

// === Responses ===

// Shape returned by GET /api/menu and GET /api/menu/{id}. The required buyer fields
// are id, name, description, unitPrice, imageUrl, availability — note the entity's
// "Price" column is deliberately surfaced as "unitPrice". Category name + stock are
// included as useful extras the frontend already consumes (grouping, stock display).
public class MenuItemResponse
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;   // category name, for buyer grouping
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }                  // <-- entity Price exposed as unitPrice
    public string ImageUrl { get; set; } = string.Empty;
    public int StockQuantity { get; set; }

    // Buyer-facing "how many can still be added" = StockQuantity minus every session's
    // active cart hold. Powers the "N left" / sold-out UI; refreshed by the menu poll.
    public int AvailableStock { get; set; }

    public bool Availability { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Body of POST /api/menu/{id}/reserve — the session's desired absolute quantity for the
// item (0 releases the hold).
public class ReserveRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "quantity must be zero or greater.")]
    public int Quantity { get; set; }
}

// Reply to a reserve/release call: how many units this session may still add.
public class ReserveResponse
{
    public int Remaining { get; set; }
}

// === Requests ===

public class CreateMenuItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid categoryId is required.")]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    // Accepted as "unitPrice" in the body, stored as the entity's Price column.
    [Range(0, double.MaxValue, ErrorMessage = "unitPrice must be zero or greater.")]
    public decimal UnitPrice { get; set; }

    // Pictures are uploaded separately via POST /api/menu/{id}/image (stored as bytes),
    // so no image field is accepted on create/update.

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool Availability { get; set; } = true;
}

public class UpdateMenuItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid categoryId is required.")]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "unitPrice must be zero or greater.")]
    public decimal UnitPrice { get; set; }

    // Pictures are uploaded separately via POST /api/menu/{id}/image (stored as bytes).

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool Availability { get; set; } = true;
}

public class UpdateAvailabilityRequest
{
    public bool Availability { get; set; }
}
