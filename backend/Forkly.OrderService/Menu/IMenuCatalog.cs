namespace Forkly.OrderService.Menu;

// The Order service's view of the Menu service. Returns null when the item does
// not exist; throws MenuUnavailableException when the menu cannot be reached.
public interface IMenuCatalog
{
    Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default);

    // Finalise a checkout: permanently decrement stock for each line and clear the
    // ordering session's holds. Atomic on the Menu side. Throws MenuUnavailableException
    // if the menu can't be reached.
    Task<StockCommit> CommitAsync(
        string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default);
}

// Outcome of a stock commit. On failure, FailedMenuId/Available identify the short line.
public readonly record struct StockCommit(bool Committed, int FailedMenuId, int Available);
