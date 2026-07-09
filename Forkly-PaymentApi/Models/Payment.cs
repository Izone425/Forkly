namespace Forkly.PaymentService.Models;

// A payment attempt for an order. Owned by the Payment service (mock, in-memory).
// The amount is copied from the Order service at checkout — never trusted from the
// client. On success the Order service is told the order is Paid.
public class Payment
{
    public string Id { get; set; } = string.Empty; // e.g. "PAY-3F9A2B7C11"
    public int OrderId { get; set; }
    public string? Reference { get; set; }          // order reference (FRK-####)
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public string Status { get; set; } = PaymentStatus.Pending;
    public string? Method { get; set; }             // "card" | "fpx" | "ewallet"
    public string? CardLast4 { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
