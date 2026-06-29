namespace Forkly.OrderService.Models;

// A single line on an order. The menu name and price are SNAPSHOTS captured at
// order time — this service does not own the menu, so it must not depend on the
// Menu service's current data to render order history.
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Reference back to the menu item this line was created from (Menu service owns it).
    public int MenuId { get; set; }

    // Snapshots taken at order time.
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public int Quantity { get; set; }
}
