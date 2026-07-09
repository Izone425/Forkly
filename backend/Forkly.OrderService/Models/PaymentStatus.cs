namespace Forkly.OrderService.Models;

// The PAYMENT state of an order, tracked independently of the fulfilment Status
// (OrderStatus). Set by the Payment service when a charge is confirmed, so the
// customer keeps seeing "Paid" even as the kitchen advances the fulfilment status.
public static class PaymentStatus
{
    public const string Unpaid = "Unpaid";  // created, not yet paid
    public const string Paid = "Paid";      // payment confirmed (Payment / Aiman)

    public static readonly string[] All = { Unpaid, Paid };

    public static bool IsValid(string status) => Array.IndexOf(All, status) >= 0;
}
