namespace Forkly.Api.Models;

// A placed order belonging to a user. This is the READ contract the separate
// ordering module will write into (it owns order creation + status changes).
// This codebase only reads orders for the account "Order history" view.
public class Order
{
    public int Id { get; set; }

    // FK → Users.Id (cascade delete configured in AppDbContext).
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Human-friendly reference, e.g. "FRK-1001" (optional; UI falls back to #Id).
    public string? Reference { get; set; }

    // One of OrderStatus.All (stored as a string for readability).
    public string Status { get; set; } = OrderStatus.Pending;

    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;

    public decimal Total { get; set; }
    public string Currency { get; set; } = "MYR";

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

// The small set of statuses an order moves through. Kept as strings so the
// ordering module and the UI share one vocabulary without an enum migration.
public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Preparing = "Preparing";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
        { Pending, Preparing, OutForDelivery, Delivered, Cancelled };
}
