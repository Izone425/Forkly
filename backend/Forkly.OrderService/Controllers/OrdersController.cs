using Forkly.OrderService.Application.DTOs;
using Forkly.OrderService.Application.Exceptions;
using Forkly.OrderService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    // POST /api/orders
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        try
        {
            var order = await _orders.CreateOrderAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { orderId = order.OrderId }, order);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/orders/{orderId}
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken ct)
    {
        var order = await _orders.GetOrderAsync(orderId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    // GET /api/orders/user/{userId}  — full order history
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserOrders(int userId, CancellationToken ct)
        => Ok(await _orders.GetUserOrdersAsync(userId, ct));

    // GET /api/orders/user/{userId}/recent  — last 3 orders for quick reorder
    [HttpGet("user/{userId:int}/recent")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentOrders(int userId, CancellationToken ct)
        => Ok(await _orders.GetRecentOrdersAsync(userId, ct));

    // POST /api/orders/reorder/{orderId}  — returns items to merge into the cart
    [HttpPost("reorder/{orderId:guid}")]
    [ProducesResponseType(typeof(ReorderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(Guid orderId, CancellationToken ct)
    {
        var result = await _orders.ReorderAsync(orderId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // PUT /api/orders/{orderId}/status
    [HttpPut("{orderId:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        try
        {
            var order = await _orders.UpdateStatusAsync(orderId, request.Status, ct);
            return order is null ? NotFound() : Ok(order);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
