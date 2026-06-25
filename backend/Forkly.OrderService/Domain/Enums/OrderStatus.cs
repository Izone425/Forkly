namespace Forkly.OrderService.Domain.Enums;

// Stored in the database as an uppercase string (CREATED, PAID, ...).
// Flow: CREATED -> PAID -> PREPARING -> READY -> COMPLETED (or CANCELLED).
public enum OrderStatus
{
    Created,
    Paid,
    Preparing,
    Ready,
    Completed,
    Cancelled,
}
