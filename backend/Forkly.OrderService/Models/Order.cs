namespace Forkly.OrderService.Models;

// An order placed by a user. Owned entirely by the Order Service and stored in
// its own isolated "order" schema in the shared foodorder database, so it never
// collides with the User service's public.Orders table.
public class Order
{
    public int Id { get; set; }

    // Human-friendly reference (e.g. "FRK-0007"), generated on create. This is the
    // code other services key on — Payment charges it, Kitchen prints it, the
    // customer quotes it. Unique. Null only in the instant between insert and the
    // follow-up update that stamps it.
    public string? Reference { get; set; }

    // The owning user's id, taken from the JWT issued by the User service (Forkly-Api).
    public int UserId { get; set; }

    // Money snapshot at the time the order was placed. Subtotal = sum(item price * qty),
    // Sst = 6% of Subtotal, Total = Subtotal + Sst.
    public decimal Subtotal { get; set; }
    public decimal Sst { get; set; }
    public decimal Total { get; set; }

    // One of OrderStatus.All, stored as a string for readability.
    public string Status { get; set; } = OrderStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
