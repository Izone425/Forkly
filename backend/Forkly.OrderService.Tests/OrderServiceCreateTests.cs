using Forkly.OrderService.Dtos;
using Forkly.OrderService.Menu;
using Forkly.OrderService.Models;
using Forkly.OrderService.Repositories;
using Xunit;

namespace Forkly.OrderService.Tests;

public class OrderServiceCreateTests
{
    private sealed class FakeMenuCatalog : IMenuCatalog
    {
        public MenuItemInfo? Result { get; init; }
        public StockCommit CommitResult { get; init; } = new(true, 0, 0);
        public Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
            => Task.FromResult(Result is null ? null : Result with { Id = menuId });
        public Task<StockCommit> CommitAsync(string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default)
            => Task.FromResult(CommitResult);
    }

    private sealed class UnavailableMenuCatalog : IMenuCatalog
    {
        public Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
            => throw new MenuUnavailableException("menu down");
        public Task<StockCommit> CommitAsync(string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default)
            => throw new MenuUnavailableException("menu down");
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Order? Saved { get; private set; }
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) { order.Id = 1; Saved = order; return Task.FromResult(order); }
        public Task UpdateAsync(Order order, CancellationToken ct = default) { Saved = order; return Task.CompletedTask; }
        public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Order?> GetByReferenceAsync(string r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Order>> GetByUserAsync(int u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Order>> GetRecentByUserAsync(int u, int c, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Order>> GetAllForReportAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<Order> Items, int Total)> GetAllAsync(string? s, int? u, int p, int ps, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task CreateAsync_uses_authoritative_menu_price_not_client_price()
    {
        var repo = new FakeOrderRepository();
        var menu = new FakeMenuCatalog { Result = new MenuItemInfo(0, "Real Burger", 20.00m, true) };
        var sut = new Forkly.OrderService.Services.OrderService(repo, menu);

        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 5, ItemName = "spoofed", Price = 0.01m, Quantity = 2 } } };
        var result = await sut.CreateAsync(userId: 42, req);

        Assert.Equal("Real Burger", result.Items[0].ItemName);
        Assert.Equal(20.00m, result.Items[0].Price);
        Assert.Equal(40.00m, result.Subtotal);   // 20.00 * 2, not 0.01 * 2
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_menu_item()
    {
        var sut = new Forkly.OrderService.Services.OrderService(new FakeOrderRepository(), new FakeMenuCatalog { Result = null });
        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 999, ItemName = "x", Price = 1m, Quantity = 1 } } };
        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(1, req));
    }

    [Fact]
    public async Task CreateAsync_rejects_unavailable_menu_item()
    {
        var sut = new Forkly.OrderService.Services.OrderService(new FakeOrderRepository(), new FakeMenuCatalog { Result = new MenuItemInfo(0, "Sold Out", 10m, false) });
        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 8, ItemName = "x", Price = 1m, Quantity = 1 } } };
        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(1, req));
    }

    [Fact]
    public async Task CreateAsync_rejects_when_stock_commit_fails()
    {
        var repo = new FakeOrderRepository();
        var menu = new FakeMenuCatalog
        {
            Result = new MenuItemInfo(0, "Teh Sorso", 5m, true),
            CommitResult = new StockCommit(false, 7, 1), // item 7 short, 1 left
        };
        var sut = new Forkly.OrderService.Services.OrderService(repo, menu);

        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 7, ItemName = "x", Price = 1m, Quantity = 3 } } };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(1, req));
        Assert.Null(repo.Saved); // no order persisted when stock is short
    }

    [Fact]
    public async Task CreateAsync_propagates_MenuUnavailable_when_menu_is_down()
    {
        var sut = new Forkly.OrderService.Services.OrderService(new FakeOrderRepository(), new UnavailableMenuCatalog());
        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 1, ItemName = "x", Price = 1m, Quantity = 1 } } };
        await Assert.ThrowsAsync<MenuUnavailableException>(() => sut.CreateAsync(1, req));
    }
}
