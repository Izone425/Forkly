using Forkly.MenuService.Dtos;
using Forkly.MenuService.Services;
using Forkly.MenuService.Services.Grpc;
using Grpc.Core;
using Xunit;

namespace ForklyMenuApi.Tests;

public class MenuGrpcServiceTests
{
    // Minimal IMenuService stub: returns a preset item for GetByIdAsync.
    private sealed class StubMenuService : IMenuService
    {
        public MenuItemResponse? Item { get; init; }
        public Task<MenuItemResponse?> GetByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(id == Item?.Id ? Item : null);
        public Task<IReadOnlyList<MenuItemResponse>> GetMenuAsync(bool availableOnly, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MenuItemResponse>>(Item is null ? new List<MenuItemResponse>() : new List<MenuItemResponse> { Item });
        public Task<MenuItemResponse> CreateAsync(CreateMenuItemRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MenuItemResponse?> UpdateAsync(int id, UpdateMenuItemRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MenuItemResponse?> SetAvailabilityAsync(int id, bool a, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public void MapToProto_converts_price_to_cents_and_id_to_string()
    {
        var dto = new MenuItemResponse { Id = 7, Name = "Burger", Description = "d", UnitPrice = 18.90m, Category = "Burger", Availability = true };
        var proto = MenuGrpcService.MapToProto(dto);
        Assert.Equal("7", proto.Id);
        Assert.Equal(1890, proto.PriceCents);
        Assert.True(proto.Available);
        Assert.Equal("Burger", proto.Category);
    }

    [Fact]
    public async Task GetItemCore_returns_mapped_item_when_found()
    {
        var svc = new MenuGrpcService(new StubMenuService { Item = new MenuItemResponse { Id = 3, Name = "Pasta", UnitPrice = 22.00m, Availability = true } });
        var result = await svc.GetItemCoreAsync("3", CancellationToken.None);
        Assert.Equal("3", result.Id);
        Assert.Equal(2200, result.PriceCents);
    }

    [Fact]
    public async Task GetItemCore_throws_NotFound_when_missing()
    {
        var svc = new MenuGrpcService(new StubMenuService { Item = null });
        var ex = await Assert.ThrowsAsync<RpcException>(() => svc.GetItemCoreAsync("999", CancellationToken.None));
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetItemCore_throws_NotFound_when_id_not_an_integer()
    {
        var svc = new MenuGrpcService(new StubMenuService { Item = null });
        var ex = await Assert.ThrowsAsync<RpcException>(() => svc.GetItemCoreAsync("abc", CancellationToken.None));
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
