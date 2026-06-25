using Forkly.OrderService.Application.Interfaces;
using Forkly.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forkly.OrderService.Infrastructure.Data;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public OrderRepository(OrderDbContext db) => _db = db;

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

    public Task<List<Order>> GetByUserAsync(int userId, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Order>> GetRecentByUserAsync(int userId, int count, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
