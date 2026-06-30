using Forkly.Contracts.Menu;
using Grpc.Core;

namespace Forkly.OrderService.Menu;

// Real catalog backed by the Menu service's gRPC endpoint (menu.proto GetItem).
public sealed class MenuGrpcCatalog : IMenuCatalog
{
    private readonly Forkly.Contracts.Menu.MenuService.MenuServiceClient _client;

    public MenuGrpcCatalog(Forkly.Contracts.Menu.MenuService.MenuServiceClient client) => _client = client;

    public async Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
    {
        try
        {
            var item = await _client.GetItemAsync(new GetItemRequest { Id = menuId.ToString() }, cancellationToken: ct);
            return new MenuItemInfo(menuId, item.Name, item.PriceCents / 100m, item.Available);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            throw new MenuUnavailableException("The menu service is unavailable. Please try again.", ex);
        }
    }
}
