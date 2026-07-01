using Forkly.Contracts.Menu;
using Forkly.OrderService.Menu;
using Grpc.Core;
using Xunit;

namespace Forkly.OrderService.Tests;

public class MenuCatalogTests
{
    // --- MenuOptions.ResolveMode (fail-closed selection) ---

    [Fact]
    public void ResolveMode_returns_Mock_when_UseMock_true()
        => Assert.Equal(MenuCatalogMode.Mock, new MenuOptions { UseMock = true }.ResolveMode());

    [Fact]
    public void ResolveMode_returns_Grpc_when_address_configured()
        => Assert.Equal(MenuCatalogMode.Grpc, new MenuOptions { UseMock = false, GrpcAddress = "http://localhost:5102" }.ResolveMode());

    [Fact]
    public void ResolveMode_throws_when_not_mock_and_no_address()
        => Assert.Throws<InvalidOperationException>(() => new MenuOptions { UseMock = false, GrpcAddress = "" }.ResolveMode());

    // --- MenuGrpcCatalog mapping + error translation (fakes the generated client) ---

    private sealed class FakeMenuClient : Forkly.Contracts.Menu.MenuService.MenuServiceClient
    {
        private readonly Func<MenuItem> _responder;
        public FakeMenuClient(Func<MenuItem> responder) : base() => _responder = responder;

        public override AsyncUnaryCall<MenuItem> GetItemAsync(GetItemRequest request, CallOptions options)
        {
            Task<MenuItem> task;
            try { task = Task.FromResult(_responder()); }
            catch (Exception ex) { task = Task.FromException<MenuItem>(ex); }
            return new AsyncUnaryCall<MenuItem>(task, Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess, () => new Metadata(), () => { });
        }
    }

    private static MenuGrpcCatalog CatalogReturning(Func<MenuItem> responder) => new(new FakeMenuClient(responder));

    [Fact]
    public async Task GetItemAsync_maps_price_cents_to_decimal()
    {
        var catalog = CatalogReturning(() => new MenuItem { Id = "5", Name = "Burger", PriceCents = 1890, Available = true });
        var info = await catalog.GetItemAsync(5);
        Assert.NotNull(info);
        Assert.Equal("Burger", info!.Name);
        Assert.Equal(18.90m, info.Price);
        Assert.True(info.Available);
    }

    [Fact]
    public async Task GetItemAsync_returns_null_on_NotFound()
    {
        var catalog = CatalogReturning(() => throw new RpcException(new Status(StatusCode.NotFound, "nope")));
        Assert.Null(await catalog.GetItemAsync(99));
    }

    [Fact]
    public async Task GetItemAsync_throws_MenuUnavailable_on_other_rpc_error()
    {
        var catalog = CatalogReturning(() => throw new RpcException(new Status(StatusCode.Unavailable, "down")));
        await Assert.ThrowsAsync<MenuUnavailableException>(() => catalog.GetItemAsync(1));
    }

    [Fact]
    public async Task GetItemAsync_throws_MenuUnavailable_on_deadline_exceeded()
    {
        var catalog = CatalogReturning(() => throw new RpcException(new Status(StatusCode.DeadlineExceeded, "slow")));
        await Assert.ThrowsAsync<MenuUnavailableException>(() => catalog.GetItemAsync(1));
    }
}
