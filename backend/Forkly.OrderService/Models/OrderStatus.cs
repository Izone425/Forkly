namespace Forkly.OrderService.Models;

// The lifecycle states an order moves through. Kept as strings (not an enum) so
// the value stored in PostgreSQL is human-readable and easy to share with the
// frontend and other services without an enum-mapping migration.
public static class OrderStatus
{
    public const string Pending = "Pending";          // created, awaiting payment (Order)
    public const string Paid = "Paid";                // payment confirmed (Payment / Aiman)
    public const string Preparing = "Preparing";      // kitchen started (Kitchen / Zul)
    public const string Completed = "Completed";      // food ready (Kitchen / Zul)
    public const string OutForDelivery = "OutForDelivery"; // rider picked up (Kitchen / Zul)
    public const string Delivered = "Delivered";      // mock timer elapsed (Tracker / Alia)
    public const string Cancelled = "Cancelled";      // cancelled from Pending/Paid

    public static readonly string[] All =
        { Pending, Paid, Preparing, Completed, OutForDelivery, Delivered, Cancelled };

    public static bool IsValid(string status) => Array.IndexOf(All, status) >= 0;
}
