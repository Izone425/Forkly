namespace Forkly.OrderService.Domain.Entities;

// Lightweight marker recorded when an order is placed, used by the reorder
// feature to quickly rebuild a cart from a previous order. Table: order_snapshots.
public class OrderSnapshot
{
    public int SnapshotId { get; set; }

    public Guid OrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Order? Order { get; set; }
}
