using Forkly.MenuService.Data;
using Forkly.MenuService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.MenuService.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly MenuDbContext _db;
    public MenuRepository(MenuDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuItem>> GetAllAsync(bool availableOnly, CancellationToken ct = default)
    {
        var query = _db.MenuItems
            .Include(m => m.Category)
            .AsQueryable();

        if (availableOnly)
            query = query.Where(m => m.Availability);

        var items = await query
            .OrderBy(m => m.CategoryId)
            .ThenBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        await PopulateImageMetaAsync(items, ct);
        return items;
    }

    public async Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.MenuItems
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (item is not null)
            await PopulateImageMetaAsync([item], ct);
        return item;
    }

    // Fills HasImage/ImageUpdatedAt from a single byte-free query so listings can
    // resolve the served image URL without ever loading image bytes.
    private async Task PopulateImageMetaAsync(IReadOnlyList<MenuItem> items, CancellationToken ct)
    {
        if (items.Count == 0) return;

        var ids = items.Select(i => i.Id).ToList();
        var meta = await _db.MenuItemImages
            .Where(x => ids.Contains(x.MenuItemId))
            .Select(x => new { x.MenuItemId, x.UpdatedAt })
            .ToDictionaryAsync(x => x.MenuItemId, x => x.UpdatedAt, ct);

        foreach (var item in items)
            if (meta.TryGetValue(item.Id, out var updatedAt))
            {
                item.HasImage = true;
                item.ImageUpdatedAt = updatedAt;
            }
    }

    public async Task SetImageAsync(int menuItemId, byte[] data, string contentType, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        var image = await _db.MenuItemImages.FirstOrDefaultAsync(x => x.MenuItemId == menuItemId, ct);
        if (image is null)
        {
            image = new MenuItemImage { MenuItemId = menuItemId };
            _db.MenuItemImages.Add(image);
        }
        image.Data = data;
        image.ContentType = contentType;
        image.UpdatedAt = updatedAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(byte[] Data, string ContentType)?> GetImageAsync(int id, CancellationToken ct = default)
    {
        var image = await _db.MenuItemImages
            .Where(x => x.MenuItemId == id)
            .Select(x => new { x.Data, x.ContentType })
            .FirstOrDefaultAsync(ct);

        if (image is null || image.Data.Length == 0) return null;
        return (image.Data, string.IsNullOrEmpty(image.ContentType) ? "image/jpeg" : image.ContentType);
    }

    public async Task<MenuItem> AddAsync(MenuItem item, CancellationToken ct = default)
    {
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public Task UpdateAsync(MenuItem item, CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);  // EF tracks the loaded entity, just persist

    public Task DeleteAsync(MenuItem item, CancellationToken ct = default)
    {
        _db.MenuItems.Remove(item);
        return _db.SaveChangesAsync(ct);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct = default) =>
        _db.Categories.AnyAsync(c => c.Id == categoryId, ct);

    // --- Stock reservations (cart holds) ---

    public async Task<IReadOnlyDictionary<int, int>> GetAvailableStockAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<int, int>();
        var now = DateTimeOffset.UtcNow;

        var stocks = await _db.MenuItems
            .Where(m => ids.Contains(m.Id))
            .Select(m => new { m.Id, m.StockQuantity })
            .ToListAsync(ct);

        // SUM of every session's active hold, per item.
        var held = await _db.StockReservations
            .Where(r => ids.Contains(r.MenuItemId) && r.ExpiresAt > now)
            .GroupBy(r => r.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Held = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.MenuItemId, x => x.Held, ct);

        var result = new Dictionary<int, int>(stocks.Count);
        foreach (var s in stocks)
        {
            held.TryGetValue(s.Id, out var h);
            var avail = s.StockQuantity - h;
            result[s.Id] = avail < 0 ? 0 : avail;
        }
        return result;
    }

    public async Task<ReserveOutcome> ReserveAsync(
        int itemId, string sessionId, int quantity, TimeSpan ttl, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Pessimistic row lock: serialize reservations for THIS item so two shoppers
        // can't both claim the last unit. Held until the transaction commits/rolls back.
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM public.\"MenuItems\" WHERE \"Id\" = {0} FOR UPDATE",
            new object[] { itemId }, ct);

        var stock = await _db.MenuItems
            .Where(m => m.Id == itemId)
            .Select(m => (int?)m.StockQuantity)
            .FirstOrDefaultAsync(ct);
        if (stock is null)
        {
            await tx.RollbackAsync(ct);
            return new ReserveOutcome(false, 0);
        }

        var takenByOthers = await _db.StockReservations
            .Where(r => r.MenuItemId == itemId && r.SessionId != sessionId && r.ExpiresAt > now)
            .SumAsync(r => (int?)r.Quantity, ct) ?? 0;

        var maxForMe = stock.Value - takenByOthers;
        if (maxForMe < 0) maxForMe = 0;

        var mine = await _db.StockReservations
            .FirstOrDefaultAsync(r => r.MenuItemId == itemId && r.SessionId == sessionId, ct);

        // Releasing (or an empty hold) always succeeds.
        if (quantity <= 0)
        {
            if (mine is not null) _db.StockReservations.Remove(mine);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new ReserveOutcome(true, maxForMe);
        }

        if (quantity > maxForMe)
        {
            await tx.RollbackAsync(ct);
            return new ReserveOutcome(false, maxForMe);
        }

        if (mine is null)
        {
            _db.StockReservations.Add(new StockReservation
            {
                MenuItemId = itemId,
                SessionId = sessionId,
                Quantity = quantity,
                CreatedAt = now,
                UpdatedAt = now,
                ExpiresAt = now + ttl,
            });
        }
        else
        {
            mine.Quantity = quantity;
            mine.UpdatedAt = now;
            mine.ExpiresAt = now + ttl;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ReserveOutcome(true, maxForMe - quantity);
    }

    public async Task ReleaseAsync(int itemId, string sessionId, CancellationToken ct = default)
    {
        var mine = await _db.StockReservations
            .FirstOrDefaultAsync(r => r.MenuItemId == itemId && r.SessionId == sessionId, ct);
        if (mine is not null)
        {
            _db.StockReservations.Remove(mine);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<CommitOutcome> CommitStockAsync(
        string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default)
    {
        // Lock rows in a stable (ascending id) order so concurrent commits can't deadlock.
        var lines = items.Where(i => i.Quantity > 0)
            .GroupBy(i => i.MenuId)
            .Select(g => (MenuId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .OrderBy(l => l.MenuId)
            .ToList();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var (menuId, qty) in lines)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM public.\"MenuItems\" WHERE \"Id\" = {0} FOR UPDATE",
                new object[] { menuId }, ct);

            var item = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuId, ct);
            if (item is null || item.StockQuantity < qty)
            {
                await tx.RollbackAsync(ct);
                return new CommitOutcome(false, menuId, item?.StockQuantity ?? 0);
            }

            item.StockQuantity -= qty;
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // The ordered units are now permanent — drop this session's holds for them.
        if (!string.IsNullOrEmpty(sessionId))
        {
            var ids = lines.Select(l => l.MenuId).ToList();
            var mine = await _db.StockReservations
                .Where(r => r.SessionId == sessionId && ids.Contains(r.MenuItemId))
                .ToListAsync(ct);
            _db.StockReservations.RemoveRange(mine);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new CommitOutcome(true, 0, 0);
    }
}
