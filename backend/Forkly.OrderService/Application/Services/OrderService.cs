using Forkly.OrderService.Application.DTOs;
using Forkly.OrderService.Application.Exceptions;
using Forkly.OrderService.Application.External;
using Forkly.OrderService.Application.Interfaces;
using Forkly.OrderService.Application.Mapping;
using Forkly.OrderService.Domain.Entities;
using Forkly.OrderService.Domain.Enums;

namespace Forkly.OrderService.Application.Services;

public class OrderService : IOrderService
{
    private const int RecentOrdersCount = 3;

    private readonly IOrderRepository _repository;
    private readonly IMenuServiceClient _menu;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository repository,
        IMenuServiceClient menu,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _menu = menu;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new ValidationException("Order must contain at least one item.");

        // TODO: gRPC calls to User Service — validate that request.UserId exists.

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = request.UserId,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0) continue;

            // TODO: gRPC calls to Menu Service — replace MockMenuServiceClient.
            var menuItem = await _menu.GetMenuItemAsync(line.MenuId, ct)
                ?? throw new ValidationException($"Menu item {line.MenuId} not found.");

            order.Items.Add(new OrderItem
            {
                MenuId = menuItem.MenuId,
                ItemName = menuItem.Name, // snapshot at order time
                Quantity = line.Quantity,
                Price = menuItem.Price,
            });
        }

        if (order.Items.Count == 0)
            throw new ValidationException("Order must contain at least one item with quantity greater than zero.");

        order.TotalAmount = order.Items.Sum(i => i.Price * i.Quantity);

        // Snapshot marker for the quick-reorder feature.
        order.Snapshots.Add(new OrderSnapshot
        {
            OrderId = order.OrderId,
            CreatedAt = DateTime.UtcNow,
        });

        await _repository.AddAsync(order, ct);

        // TODO: event push to Alia tracking service — notify "order created".

        _logger.LogInformation(
            "Created order {OrderId} for user {UserId} ({LineCount} lines, total {Total})",
            order.OrderId, order.UserId, order.Items.Count, order.TotalAmount);

        return order.ToDto();
    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        return order?.ToDto();
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(int userId, CancellationToken ct = default)
    {
        var orders = await _repository.GetByUserAsync(userId, ct);
        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<List<OrderDto>> GetRecentOrdersAsync(int userId, CancellationToken ct = default)
    {
        var orders = await _repository.GetRecentByUserAsync(userId, RecentOrdersCount, ct);
        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<ReorderResponseDto?> ReorderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        if (order is null) return null;

        // IMPORTANT: do NOT create a new order. Return the items so the frontend
        // can merge them into the current cart.
        return new ReorderResponseDto
        {
            SourceOrderId = order.OrderId,
            SourceCreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => i.ToDto()).ToList(),
        };
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid orderId, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            throw new ValidationException($"Invalid status '{status}'.");

        var order = await _repository.GetByIdAsync(orderId, ct);
        if (order is null) return null;

        order.Status = parsed;
        await _repository.SaveChangesAsync(ct);

        // TODO: event push to Alia tracking service — notify status change.

        _logger.LogInformation("Order {OrderId} status updated to {Status}", order.OrderId, parsed);
        return order.ToDto();
    }
}
