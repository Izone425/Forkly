using Forkly.OrderService.Models;

namespace Forkly.OrderService.Repositories;

// Data-access boundary for orders. Keeps EF Core out of the service layer.
public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken ct = default);

    // Persists changes to an already-tracked order (e.g. reference stamp, status change).
    Task UpdateAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);

    // Lookup by the human-friendly reference (used by Payment / Kitchen).
    Task<Order?> GetByReferenceAsync(string reference, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetByUserAsync(int userId, CancellationToken ct = default);

    // Newest-first, capped at `count` (used for the "recent orders" view).
    Task<IReadOnlyList<Order>> GetRecentByUserAsync(int userId, int count, CancellationToken ct = default);

    // All non-cancelled orders (with items) across all users, for reporting.
    Task<IReadOnlyList<Order>> GetAllForReportAsync(CancellationToken ct = default);

    // Admin: a single page of orders across ALL users (newest first), with the
    // matching total count. Optional filters by status and/or user id. Unlike the
    // report query this includes Cancelled orders.
    Task<(IReadOnlyList<Order> Items, int Total)> GetAllAsync(
        string? status, int? userId, int page, int pageSize, CancellationToken ct = default);
}
