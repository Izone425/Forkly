using Forkly.OrderService.Domain.Enums;

namespace Forkly.OrderService.Domain.Entities;

// Aggregate root for an order. Persisted via EF Core (table: orders).
public class Order
{
    public Guid OrderId { get; set; }

    // Mock external id for now; the User Service (Izzuwan) owns users.
    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Created;

    public DateTime CreatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    public List<OrderSnapshot> Snapshots { get; set; } = new();
}
