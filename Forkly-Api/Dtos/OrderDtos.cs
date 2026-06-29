namespace Forkly.Api.Dtos;

// Read-only shapes for the account "Order history" view.

public class OrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string? Reference { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset PlacedAt { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "MYR";
    public List<OrderItemDto> Items { get; set; } = new();
}
