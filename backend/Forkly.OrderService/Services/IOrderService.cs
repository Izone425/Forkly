using Forkly.OrderService.Dtos;

namespace Forkly.OrderService.Services;

// Business logic boundary. Controllers depend on this, never on EF entities.
public interface IOrderService
{
    Task<OrderResponse> CreateAsync(int userId, CreateOrderRequest request, CancellationToken ct = default);

    // Returns null if the order does not exist or does not belong to the caller.
    Task<OrderResponse?> GetByIdAsync(int orderId, int userId, CancellationToken ct = default);

    Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(int userId, CancellationToken ct = default);

    Task<IReadOnlyList<OrderResponse>> GetRecentAsync(int userId, int count, CancellationToken ct = default);

    // Service/staff-facing (Payment, Kitchen): look up any order by its reference.
    Task<OrderResponse?> GetByReferenceAsync(string reference, CancellationToken ct = default);

    // Kitchen (crew/admin): active orders for the kitchen board, oldest first.
    Task<IReadOnlyList<OrderResponse>> GetKitchenQueueAsync(CancellationToken ct = default);

    // Service/staff-facing: advance an order's fulfilment status. Returns null if not
    // found, throws ArgumentException if the status value is not a known OrderStatus.
    Task<OrderResponse?> UpdateStatusAsync(int orderId, string status, CancellationToken ct = default);

    // Payment (Aiman): update an order's payment status (→ Paid). Returns null if not
    // found, throws ArgumentException if the value is not a known PaymentStatus.
    Task<OrderResponse?> UpdatePaymentStatusAsync(int orderId, string paymentStatus, CancellationToken ct = default);

    // Returns the source order's items for the frontend to merge into the cart.
    // Writes nothing. Null if the order does not exist or is not the caller's.
    Task<ReorderResponse?> ReorderAsync(int orderId, int userId, CancellationToken ct = default);

    // Admin: a page of orders across all users, newest first, optionally filtered
    // by status and/or user id. Caller authorization is enforced at the controller.
    Task<PagedResult<OrderResponse>> GetAllAsync(
        string? status, int? userId, int page, int pageSize, CancellationToken ct = default);
}
