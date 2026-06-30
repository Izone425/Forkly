using Forkly.Contracts.Menu;
using Forkly.MenuService.Dtos;
using Forkly.MenuService.Services;
using Grpc.Core;

namespace Forkly.MenuService.Services.Grpc;

// gRPC facade over IMenuService — the parallel of MenuController. The base class is
// generated from backend/contracts/menu.proto by Grpc.Tools at build. The Order
// service is the client (it validates + prices a cart line through GetItem).
public class MenuGrpcService : Forkly.Contracts.Menu.MenuService.MenuServiceBase
{
    private readonly IMenuService _menu;

    public MenuGrpcService(IMenuService menu) => _menu = menu;

    public override async Task<GetMenuResponse> GetMenu(GetMenuRequest request, ServerCallContext context)
    {
        var items = await _menu.GetMenuAsync(availableOnly: true, context.CancellationToken);
        var response = new GetMenuResponse();
        response.Items.AddRange(items.Select(MapToProto));
        return response;
    }

    public override Task<Forkly.Contracts.Menu.MenuItem> GetItem(GetItemRequest request, ServerCallContext context)
        => GetItemCoreAsync(request.Id, context.CancellationToken);

    // Transport-free core so the lookup/not-found logic is unit-testable without a
    // ServerCallContext. Returns the mapped item or throws RpcException(NotFound).
    public async Task<Forkly.Contracts.Menu.MenuItem> GetItemCoreAsync(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var menuId))
            throw new RpcException(new Status(StatusCode.NotFound, $"Menu item '{id}' not found."));

        var item = await _menu.GetByIdAsync(menuId, ct);
        if (item is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Menu item '{id}' not found."));

        return MapToProto(item);
    }

    // Entity-DTO -> proto. Price (decimal RM) -> price_cents (int). The entity has no
    // emoji (it uses ImageUrl), and consumers don't need it, so emoji is empty.
    public static Forkly.Contracts.Menu.MenuItem MapToProto(MenuItemResponse m) => new()
    {
        Id = m.Id.ToString(),
        Name = m.Name,
        Description = m.Description,
        PriceCents = (int)decimal.Round(m.UnitPrice * 100m, 0, MidpointRounding.AwayFromZero),
        Category = m.Category,
        Emoji = string.Empty,
        Available = m.Availability,
    };
}
