using Forkly.OrderService.Application.DTOs;
using Forkly.OrderService.Domain.Entities;

namespace Forkly.OrderService.Application.Mapping;

// Manual entity -> DTO mapping (EF models are never returned from the API).
public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new()
    {
        OrderId = order.OrderId,
        UserId = order.UserId,
        TotalAmount = order.TotalAmount,
        Status = order.Status.ToString().ToUpperInvariant(),
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => i.ToDto()).ToList(),
    };

    public static OrderItemDto ToDto(this OrderItem item) => new()
    {
        MenuId = item.MenuId,
        ItemName = item.ItemName,
        Quantity = item.Quantity,
        Price = item.Price,
    };
}
