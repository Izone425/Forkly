namespace Forkly.OrderService.Domain.Entities;

// A single priced line in an order. ItemName/Price are a SNAPSHOT taken at
// order time (the Menu Service may change later). Table: order_items.
public class OrderItem
{
    public int OrderItemId { get; set; }

    public Guid OrderId { get; set; }

    // Mock external id for now; the Menu Service (amirul) owns menu items.
    public int MenuId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public Order? Order { get; set; }
}
