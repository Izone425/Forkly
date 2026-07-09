using System.ComponentModel.DataAnnotations;

namespace Forkly.PaymentService.Dtos;

// ---------- Requests ----------

// Start a payment for an order that already exists in the Order service.
public class CheckoutRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid orderId is required.")]
    public int OrderId { get; set; }
}

// Settle a payment (mock gateway — no real charge happens).
public class PayRequest
{
    // Display-only for the mock: "card" | "fpx" | "ewallet".
    public string Method { get; set; } = "card";

    // Last 4 digits of the (fake) card, shown on the receipt.
    public string? CardLast4 { get; set; }

    // Test hook: force a declined payment to exercise the failure path.
    public bool SimulateFailure { get; set; }
}

// ---------- Response ----------

public class PaymentResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public string Status { get; set; } = string.Empty; // Pending | Paid | Failed
    public string? Method { get; set; }
    public string? CardLast4 { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
