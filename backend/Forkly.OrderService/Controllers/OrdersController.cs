using System.Security.Claims;
using Forkly.OrderService.Dtos;
using Forkly.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.OrderService.Controllers;

// All endpoints require a valid JWT issued by the User service (Forkly-Api).
// The caller's user id is read from the token, never from the request body.
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private const int RecentCount = 3;

    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    // POST /api/orders — create an order from the cart; server computes SST + total.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var created = await _orders.CreateAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetById), new { orderId = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/orders/{orderId} — full order with items (only the caller's own).
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetById(int orderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var order = await _orders.GetByIdAsync(orderId, userId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    // GET /api/orders/user/{userId} — all of a user's orders, newest first.
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserOrders(int userId, CancellationToken ct)
    {
        if (!TryGetUserId(out var current)) return Unauthorized();
        if (current != userId) return Forbid();

        return Ok(await _orders.GetUserOrdersAsync(userId, ct));
    }

    // GET /api/orders/user/{userId}/recent — the user's 3 most recent orders.
    [HttpGet("user/{userId:int}/recent")]
    public async Task<IActionResult> GetRecent(int userId, CancellationToken ct)
    {
        if (!TryGetUserId(out var current)) return Unauthorized();
        if (current != userId) return Forbid();

        return Ok(await _orders.GetRecentAsync(userId, RecentCount, ct));
    }

    // ---- Service / staff-facing (Payment · Kitchen · Admin) ----
    // These identify an order by reference / id rather than the caller, so they do
    // not enforce owner-match. They still require a valid JWT.

    // GET /api/orders/reference/{reference} — look up an order by its FRK-#### code.
    [HttpGet("reference/{reference}")]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken ct)
    {
        var order = await _orders.GetByReferenceAsync(reference, ct);
        return order is null ? NotFound() : Ok(order);
    }

    // PATCH /api/orders/{orderId}/status — advance the order's status.
    // Used by Payment (→ Paid), Kitchen (→ Preparing/Completed/OutForDelivery),
    // and Tracker (→ Delivered).
    [HttpPatch("{orderId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await _orders.UpdateStatusAsync(orderId, request.Status, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST /api/orders/reorder/{orderId} — return a past order's items for the
    // frontend to merge into the cart. Writes nothing to the database.
    [HttpPost("reorder/{orderId:int}")]
    public async Task<IActionResult> Reorder(int orderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _orders.ReorderAsync(orderId, userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // The User service signs the standard NameIdentifier ("sub") claim with the user id.
    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(raw, out userId);
    }
}
