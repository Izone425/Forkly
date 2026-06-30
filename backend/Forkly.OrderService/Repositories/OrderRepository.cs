using Forkly.OrderService.Data;
using Forkly.OrderService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.OrderService.Repositories;

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

    public Task UpdateAsync(Order order, CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    public Task<Order?> GetByReferenceAsync(string reference, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Reference == reference, ct);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(int userId, CancellationToken ct = default) =>
        await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetRecentByUserAsync(int userId, int count, CancellationToken ct = default) =>
        await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetAllForReportAsync(CancellationToken ct = default) =>
        await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Order> Items, int Total)> GetAllAsync(
        string? status, int? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);
        if (userId is not null)
            query = query.Where(o => o.UserId == userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }
}
