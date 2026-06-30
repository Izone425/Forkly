namespace Forkly.OrderService.Menu;

// The Order service's view of the Menu service. Returns null when the item does
// not exist; throws MenuUnavailableException when the menu cannot be reached.
public interface IMenuCatalog
{
    Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default);
}
