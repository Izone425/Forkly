namespace Forkly.OrderService.Application.DTOs;

// --- Requests (inbound) ---

public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public int MenuId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// --- Responses (outbound) — EF entities are never exposed directly ---

public class OrderDto
{
    public Guid OrderId { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int MenuId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

// Returned by the reorder endpoint. Contains the items to MERGE into the cart
// on the frontend — the service does NOT create a new order here.
public class ReorderResponseDto
{
    public Guid SourceOrderId { get; set; }
    public DateTime SourceCreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
