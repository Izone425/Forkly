using Forkly.MenuService.Models;

namespace Forkly.MenuService.Repositories;

// Data-access boundary for menu items. Keeps EF Core out of the service layer.
public interface IMenuRepository
{
    Task<IReadOnlyList<MenuItem>> GetAllAsync(bool availableOnly, CancellationToken ct = default);
    Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MenuItem> AddAsync(MenuItem item, CancellationToken ct = default);
    Task UpdateAsync(MenuItem item, CancellationToken ct = default);
    Task DeleteAsync(MenuItem item, CancellationToken ct = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct = default);

    // Upsert the uploaded picture bytes for a menu item (1:1).
    Task SetImageAsync(int menuItemId, byte[] data, string contentType, DateTimeOffset updatedAt, CancellationToken ct = default);

    // Read a menu item's stored picture bytes + content type, or null if none.
    Task<(byte[] Data, string ContentType)?> GetImageAsync(int id, CancellationToken ct = default);

    // --- Stock reservations (cart holds) ---

    // For each id, available buyer stock = StockQuantity - SUM(active holds across all
    // sessions), clamped at 0. Ids with no MenuItem are simply absent from the result.
    Task<IReadOnlyDictionary<int, int>> GetAvailableStockAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct = default);

    // Set this session's absolute hold for an item to `quantity` (0 releases it), inside a
    // per-item locked transaction so concurrent shoppers can't oversell. Returns whether it
    // was accepted and how much this session could still add afterward.
    Task<ReserveOutcome> ReserveAsync(
        int itemId, string sessionId, int quantity, TimeSpan ttl, CancellationToken ct = default);

    // Drop this session's hold for an item.
    Task ReleaseAsync(int itemId, string sessionId, CancellationToken ct = default);

    // Finalise a checkout: atomically decrement StockQuantity for each line and delete
    // the session's holds for those items. If any line lacks stock, nothing is changed.
    Task<CommitOutcome> CommitStockAsync(
        string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default);
}

// Result of a reserve attempt. Remaining = how many more units this session may still
// hold for the item (already excludes this session's own accepted hold).
public readonly record struct ReserveOutcome(bool Accepted, int Remaining);

// Result of a stock commit. On failure, FailedMenuId/Available identify the short line.
public readonly record struct CommitOutcome(bool Committed, int FailedMenuId, int Available);
