namespace OrderService.Api;

// REST/JSON shapes for the browser. Money is in major units (RM, decimal) so
// the frontend can show it directly; the backend works in cents internally.

public record MenuItemResponse(
    string Id, string Name, string Description,
    decimal Price, string Category, string Emoji, bool Available);

public sealed class CreateOrderRequestDto
{
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public sealed class CreateOrderItemDto
{
    public string Id { get; set; } = "";
    public int Quantity { get; set; }
}

public record OrderItemResponse(
    string Id, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderResponse(
    string OrderId,
    List<OrderItemResponse> Items,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    string Status,
    string? PaymentId,
    string? PaymentRedirectUrl);

// cents -> RM (decimal). Single conversion point.
public static class Money
{
    public static decimal Rm(int cents) => cents / 100m;
}
