namespace Forkly.OrderService.Application.External;

// Minimal menu info the Order Service needs to price/snapshot a line.
public record MenuItemInfo(int MenuId, string Name, decimal Price);

// Abstraction over the Menu Service (amirul). Today: MockMenuServiceClient.
// Later: a gRPC client (MenuService.GetMenuItem).
public interface IMenuServiceClient
{
    Task<MenuItemInfo?> GetMenuItemAsync(int menuId, CancellationToken ct = default);
}
