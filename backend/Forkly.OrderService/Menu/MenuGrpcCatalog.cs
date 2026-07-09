using Forkly.Contracts.Menu;
using Grpc.Core;

namespace Forkly.OrderService.Menu;

// Real catalog backed by the Menu service's gRPC endpoint (menu.proto GetItem).
public sealed class MenuGrpcCatalog : IMenuCatalog
{
    private readonly Forkly.Contracts.Menu.MenuService.MenuServiceClient _client;

    public MenuGrpcCatalog(Forkly.Contracts.Menu.MenuService.MenuServiceClient client) => _client = client;

    // Bounds the wait on the synchronous order-create money path: a hung menu service
    // surfaces as DeadlineExceeded (mapped to MenuUnavailableException) instead of
    // blocking until the HttpClient default timeout.
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(5);

    public async Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
    {
        try
        {
            var options = new CallOptions(deadline: DateTime.UtcNow.Add(CallTimeout), cancellationToken: ct);
            var item = await _client.GetItemAsync(new GetItemRequest { Id = menuId.ToString() }, options);
            return new MenuItemInfo(menuId, item.Name, item.PriceCents / 100m, item.Available);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && ct.IsCancellationRequested)
        {
            // Caller aborted — propagate as cancellation, not a service outage.
            throw new OperationCanceledException(ct);
        }
        catch (RpcException ex)
        {
            // Unavailable, DeadlineExceeded, Internal, etc. — the menu can't be reached.
            throw new MenuUnavailableException("The menu service is unavailable. Please try again.", ex);
        }
    }

    public async Task<StockCommit> CommitAsync(
        string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default)
    {
        var request = new CommitStockRequest { SessionId = sessionId ?? string.Empty };
        request.Items.AddRange(items.Select(i => new CommitStockLine { MenuId = i.MenuId, Quantity = i.Quantity }));

        try
        {
            var options = new CallOptions(deadline: DateTime.UtcNow.Add(CallTimeout), cancellationToken: ct);
            var res = await _client.CommitStockAsync(request, options);
            return new StockCommit(res.Committed, res.FailedMenuId, res.Available);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }
        catch (RpcException ex)
        {
            throw new MenuUnavailableException("The menu service is unavailable. Please try again.", ex);
        }
    }
}
