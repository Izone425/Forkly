using Forkly.OrderService.Dtos;
using Forkly.OrderService.Menu;
using Forkly.OrderService.Models;
using Forkly.OrderService.Repositories;

namespace Forkly.OrderService.Services;

public class OrderService : IOrderService
{
    // Malaysian SST on prepared food/beverage is 6%. Single source of truth.
    private const decimal SstRate = 0.06m;

    private readonly IOrderRepository _repo;
    private readonly IMenuCatalog _menu;

    public OrderService(IOrderRepository repo, IMenuCatalog menu)
    {
        _repo = repo;
        _menu = menu;
    }

    public async Task<OrderResponse> CreateAsync(int userId, CreateOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("An order must contain at least one item.");

        var now = DateTimeOffset.UtcNow;

        // Validate each line against the Menu service over gRPC and take the menu's
        // authoritative name + price — the client-sent name/price are never trusted.
        var items = new List<OrderItem>();
        foreach (var i in request.Items)
        {
            var info = await _menu.GetItemAsync(i.MenuId, ct);
            if (info is null)
                throw new ArgumentException($"Menu item {i.MenuId} does not exist.");
            if (!info.Available)
                throw new ArgumentException($"'{info.Name}' is currently unavailable.");

            items.Add(new OrderItem
            {
                MenuId = i.MenuId,
                ItemName = info.Name,   // authoritative name from the Menu service
                Price = info.Price,     // authoritative price — the client-sent price is ignored
                Quantity = i.Quantity,
            });
        }

        // Reserve the physical stock: the Menu service atomically decrements each line
        // (and clears this cart's holds). If anything is short, no order is created.
        var commitLines = items.Select(i => (i.MenuId, i.Quantity)).ToList();
        var commit = await _menu.CommitAsync(request.SessionId ?? string.Empty, commitLines, ct);
        if (!commit.Committed)
        {
            var shortName = items.FirstOrDefault(i => i.MenuId == commit.FailedMenuId)?.ItemName ?? "An item";
            throw new ArgumentException(
                $"'{shortName}' is out of stock — only {commit.Available} left.");
        }

        // Money is computed server-side from the line snapshots; never trust client totals.
        // Half-up rounding to match printed-receipt conventions (not banker's rounding).
        var subtotal = decimal.Round(items.Sum(i => i.Price * i.Quantity), 2, MidpointRounding.AwayFromZero);
        var sst = decimal.Round(subtotal * SstRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + sst;

        var order = new Order
        {
            UserId = userId,
            Subtotal = subtotal,
            Sst = sst,
            Total = total,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            Items = items,
        };

        var saved = await _repo.AddAsync(order, ct);

        // Stamp the human-friendly reference now that the id exists, then persist.
        // Derived from the unique id, so it is itself unique (no collision risk).
        saved.Reference = $"FRK-{saved.Id:D4}";
        await _repo.UpdateAsync(saved, ct);

        return Map(saved);
    }

    public async Task<OrderResponse?> GetByIdAsync(int orderId, int userId, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId) return null;
        return Map(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(int userId, CancellationToken ct = default)
    {
        var orders = await _repo.GetByUserAsync(userId, ct);
        return orders.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetRecentAsync(int userId, int count, CancellationToken ct = default)
    {
        var orders = await _repo.GetRecentByUserAsync(userId, count, ct);
        return orders.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetKitchenQueueAsync(CancellationToken ct = default)
    {
        var orders = await _repo.GetKitchenQueueAsync(ct);
        return orders.Select(Map).ToList();
    }

    public async Task<OrderResponse?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var order = await _repo.GetByReferenceAsync(reference, ct);
        return order is null ? null : Map(order);
    }

    public async Task<OrderResponse?> UpdateStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        if (!OrderStatus.IsValid(status))
            throw new ArgumentException(
                $"Unknown status '{status}'. Valid values: {string.Join(", ", OrderStatus.All)}.");

        var order = await _repo.GetByIdAsync(orderId, ct);
        if (order is null) return null;

        order.Status = status;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(order, ct);

        return Map(order);
    }

    public async Task<OrderResponse?> UpdatePaymentStatusAsync(int orderId, string paymentStatus, CancellationToken ct = default)
    {
        if (!PaymentStatus.IsValid(paymentStatus))
            throw new ArgumentException(
                $"Unknown payment status '{paymentStatus}'. Valid values: {string.Join(", ", PaymentStatus.All)}.");

        var order = await _repo.GetByIdAsync(orderId, ct);
        if (order is null) return null;

        // Payment is orthogonal to fulfilment — only PaymentStatus changes here.
        order.PaymentStatus = paymentStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(order, ct);

        return Map(order);
    }

    public async Task<ReorderResponse?> ReorderAsync(int orderId, int userId, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId) return null;

        // Read-only: hand the previous order's lines back for the frontend to merge
        // into the cart. We deliberately do NOT create a new order here.
        return new ReorderResponse
        {
            SourceOrderId = order.Id,
            Items = order.Items
                .Select(i => new CartItemDto
                {
                    MenuId = i.MenuId,
                    ItemName = i.ItemName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                })
                .ToList(),
        };
    }

    public async Task<PagedResult<OrderResponse>> GetAllAsync(
        string? status, int? userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // Reject an unknown status filter rather than silently returning everything.
        if (!string.IsNullOrWhiteSpace(status) && !OrderStatus.IsValid(status))
            throw new ArgumentException(
                $"Unknown status '{status}'. Valid values: {string.Join(", ", OrderStatus.All)}.");

        var (orders, total) = await _repo.GetAllAsync(status, userId, page, pageSize, ct);

        return new PagedResult<OrderResponse>
        {
            Items = orders.Select(Map).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    private static OrderResponse Map(Order o) => new()
    {
        Id = o.Id,
        Reference = o.Reference,
        UserId = o.UserId,
        Subtotal = o.Subtotal,
        Sst = o.Sst,
        Total = o.Total,
        Status = o.Status,
        PaymentStatus = o.PaymentStatus,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items
            .Select(i => new OrderItemResponse
            {
                Id = i.Id,
                MenuId = i.MenuId,
                ItemName = i.ItemName,
                Price = i.Price,
                Quantity = i.Quantity,
            })
            .ToList(),
    };
}
