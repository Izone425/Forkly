using Forkly.TrackerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.TrackerService.Controllers;

// Customer order tracking (Alia). Any signed-in user can track THEIR OWN order —
// the Order service enforces ownership on the forwarded token.
[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _tracking;

    public TrackingController(ITrackingService tracking) => _tracking = tracking;

    // GET /api/tracking/{orderId} — live status, progress steps, and (while Out for
    // delivery) the mock ETA. Auto-marks the order Delivered when the timer elapses.
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> Get(int orderId, CancellationToken ct)
    {
        if (!TryToken(out var token)) return Unauthorized();

        var tracking = await _tracking.GetAsync(orderId, token, ct);
        return tracking is null ? NotFound() : Ok(tracking);
    }

    private bool TryToken(out string token)
    {
        token = string.Empty;
        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = auth["Bearer ".Length..].Trim();
        return token.Length > 0;
    }
}
