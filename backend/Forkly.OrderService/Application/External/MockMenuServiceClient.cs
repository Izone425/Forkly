namespace Forkly.OrderService.Application.External;

// Stand-in for the Menu Service until gRPC is wired up.
//
// TODO: gRPC calls to Menu Service — replace this mock with a real client
//       (e.g. MenuService.GetMenuItem(menuId)).
public class MockMenuServiceClient : IMenuServiceClient
{
    private static readonly Dictionary<int, MenuItemInfo> Menu = new()
    {
        [1] = new MenuItemInfo(1, "Classic Burger", 10.00m),
        [2] = new MenuItemInfo(2, "Chicken Wings", 12.00m),
        [3] = new MenuItemInfo(3, "Fries", 5.00m),
        [4] = new MenuItemInfo(4, "Ice Cream Sundae", 7.00m),
        [5] = new MenuItemInfo(5, "Coffee", 6.00m),
        [6] = new MenuItemInfo(6, "Soft Drink", 4.00m),
    };

    public Task<MenuItemInfo?> GetMenuItemAsync(int menuId, CancellationToken ct = default)
        => Task.FromResult(Menu.TryGetValue(menuId, out var item) ? item : null);
}
