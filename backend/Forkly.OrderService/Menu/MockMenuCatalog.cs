namespace Forkly.OrderService.Menu;

// Offline stand-in used when Menu:UseMock=true (or no GrpcAddress). Echoes the
// requested item so local demos run without the Menu service; price is NOT
// authoritative (0). Do not use where real pricing matters.
public sealed class MockMenuCatalog : IMenuCatalog
{
    public Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
        => Task.FromResult<MenuItemInfo?>(new MenuItemInfo(menuId, $"Item {menuId}", 0m, true));

    // No stock tracking in the offline mock — always "commits".
    public Task<StockCommit> CommitAsync(
        string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default)
        => Task.FromResult(new StockCommit(true, 0, 0));
}
