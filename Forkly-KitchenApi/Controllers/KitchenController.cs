using Forkly.KitchenService.Dtos;
using Forkly.KitchenService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.KitchenService.Controllers;

// Kitchen display + actions. Crew and admins only (role from the User service's JWT).
[ApiController]
[Route("api/kitchen")]
[Authorize(Roles = "crew,admin")]
public class KitchenController : ControllerBase
{
    private readonly IKitchenService _kitchen;

    public KitchenController(IKitchenService kitchen) => _kitchen = kitchen;

    // GET /api/kitchen/queue — active tickets (New/Preparing/Completed/OutForDelivery).
    [HttpGet("queue")]
    public async Task<IActionResult> Queue(CancellationToken ct)
    {
        if (!TryToken(out var token)) return Unauthorized();
        return Ok(await _kitchen.GetQueueAsync(token, ct));
    }

    // POST /api/kitchen/orders/{orderId}/status — crew advances a ticket:
    // Start (Preparing), Ready (Completed), or Out for delivery (OutForDelivery).
    [HttpPost("orders/{orderId:int}/status")]
    public async Task<IActionResult> SetStatus(int orderId, [FromBody] StatusRequest request, CancellationToken ct)
    {
        if (!TryToken(out var token)) return Unauthorized();
        try
        {
            var ticket = await _kitchen.SetStatusAsync(orderId, request.Status, token, ct);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // The raw bearer token to forward to the Order service.
    private bool TryToken(out string token)
    {
        token = string.Empty;
        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = auth["Bearer ".Length..].Trim();
        return token.Length > 0;
    }
}
