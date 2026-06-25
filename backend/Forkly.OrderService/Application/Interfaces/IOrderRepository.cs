using Forkly.OrderService.Domain.Entities;

namespace Forkly.OrderService.Application.Interfaces;

// Data-access contract. Implemented by Infrastructure (EF Core / PostgreSQL).
public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    Task<List<Order>> GetByUserAsync(int userId, CancellationToken ct = default);

    Task<List<Order>> GetRecentByUserAsync(int userId, int count, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
