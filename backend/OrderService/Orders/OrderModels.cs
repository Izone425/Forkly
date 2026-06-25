namespace OrderService.Orders;

// One priced line in an order. Money is in cents.
public record OrderLine(string Id, string Name, int UnitPriceCents, int Quantity)
{
    public int LineTotalCents => UnitPriceCents * Quantity;
}

// A stored order. Pricing is computed server-side from the menu service —
// never trusted from the client.
public sealed class OrderRecord
{
    public required string OrderId { get; init; }
    public required IReadOnlyList<OrderLine> Items { get; init; }
    public int SubtotalCents { get; init; }
    public int TaxCents { get; init; }
    public int TotalCents { get; init; }
    public string Status { get; set; } = "CREATED";
    public string? PaymentId { get; set; }
    public string? PaymentRedirectUrl { get; set; }
}

// Thrown when a cart can't be turned into a valid order (unknown/unavailable
// item, empty cart). Surfaces as 400 (REST) / InvalidArgument (gRPC).
public sealed class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message) { }
}
