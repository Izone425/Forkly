using System.ComponentModel.DataAnnotations;

namespace Forkly.OrderService.Dtos;

// ---------- Requests ----------

// Cart payload sent by the frontend when placing an order. The server recomputes
// all money (subtotal/SST/total) from these lines — client-sent totals are ignored.
public class CreateOrderRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "An order must contain at least one item.")]
    public List<CreateOrderItemDto> Items { get; set; } = new();

    // Cart/session id (X-Forkly-Session) that held the stock while shopping. Passed to
    // the Menu service at checkout so it decrements stock and clears this cart's holds.
    public string? SessionId { get; set; }
}

public class CreateOrderItemDto
{
    public int MenuId { get; set; }

    [Required]
    public string ItemName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}

// ---------- Responses ----------

public class OrderResponse
{
    public int Id { get; set; }
    public string? Reference { get; set; }
    public int UserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Sst { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "MYR";
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

// Sent by Payment (Aiman) / Kitchen (Zul) to advance an order's status.
public class UpdateStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class OrderItemResponse
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

// Generic paged envelope — used by the admin "all orders" listing.
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Returned by the reorder endpoint. These items are meant to be merged into the
// cart by the frontend; the reorder call itself writes nothing to the database.
public class ReorderResponse
{
    public int SourceOrderId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
}

public class CartItemDto
{
    public int MenuId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
