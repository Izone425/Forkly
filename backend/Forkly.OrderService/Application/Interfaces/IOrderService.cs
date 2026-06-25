using Forkly.OrderService.Application.DTOs;

namespace Forkly.OrderService.Application.Interfaces;

// Application/business contract used by controllers. Works only with DTOs.
public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);

    Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<List<OrderDto>> GetUserOrdersAsync(int userId, CancellationToken ct = default);

    Task<List<OrderDto>> GetRecentOrdersAsync(int userId, CancellationToken ct = default);

    // Returns the previous order's items to merge into the cart (no new order).
    Task<ReorderResponseDto?> ReorderAsync(Guid orderId, CancellationToken ct = default);

    Task<OrderDto?> UpdateStatusAsync(Guid orderId, string status, CancellationToken ct = default);
}
