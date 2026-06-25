using Forkly.Contracts.Menu;
using Grpc.Core;

namespace OrderService.Menu;

// Real client for amirul-menu's gRPC service. Wraps the generated
// MenuService.MenuServiceClient and maps proto messages to MenuItemDto.
// Not used until Menu:UseMock=false (see Program.cs).
public sealed class MenuGrpcClient : IMenuClient
{
    private readonly MenuService.MenuServiceClient _client;

    public MenuGrpcClient(MenuService.MenuServiceClient client) => _client = client;

    public async Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(CancellationToken ct = default)
    {
        var resp = await _client.GetMenuAsync(new GetMenuRequest(), cancellationToken: ct);
        return resp.Items.Select(Map).ToList();
    }

    public async Task<MenuItemDto?> GetItemAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var item = await _client.GetItemAsync(new GetItemRequest { Id = id }, cancellationToken: ct);
            return Map(item);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private static MenuItemDto Map(MenuItem m) =>
        new(m.Id, m.Name, m.Description, m.PriceCents, m.Category, m.Emoji, m.Available);
}
