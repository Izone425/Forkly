using Forkly.MenuService.Dtos;
using Forkly.MenuService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.MenuService.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menu;
    public MenuController(IMenuService menu) => _menu = menu;

    // --- Buyer (public) ---

    // GET /api/menu — buyer menu listing. Returns only available items so the
    // storefront never shows sold-out/hidden dishes.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetMenu(CancellationToken ct) =>
        Ok(await _menu.GetMenuAsync(availableOnly: true, ct));

    // GET /api/menu/{id} — buyer item details.
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _menu.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    // --- Cart stock holds (public; keyed by the X-Forkly-Session header) ---

    // Header the browser sends to identify a guest/user cart across reserve/release calls.
    private const string SessionHeader = "X-Forkly-Session";

    // POST /api/menu/{id}/reserve — set this session's hold for the item to the given
    // quantity (0 releases it). 200 with how many the session may still add, or 409 when
    // there isn't enough stock left once other shoppers' holds are counted.
    [HttpPost("{id:int}/reserve")]
    [AllowAnonymous]
    public async Task<IActionResult> Reserve(int id, [FromBody] ReserveRequest request, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(SessionHeader, out var sid) || string.IsNullOrWhiteSpace(sid))
            return BadRequest(new { error = $"Missing {SessionHeader} header." });

        var result = await _menu.ReserveAsync(id, sid.ToString(), request.Quantity, ct);
        return result.Status switch
        {
            ReservationStatus.Accepted => Ok(new ReserveResponse { Remaining = result.Remaining }),
            ReservationStatus.Insufficient => Conflict(new ReserveResponse { Remaining = result.Remaining }),
            ReservationStatus.Unavailable => Conflict(new { error = "This item is currently unavailable." }),
            _ => NotFound(),
        };
    }

    // DELETE /api/menu/{id}/reserve — drop this session's hold for the item.
    [HttpDelete("{id:int}/reserve")]
    [AllowAnonymous]
    public async Task<IActionResult> Release(int id, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(SessionHeader, out var sid) || string.IsNullOrWhiteSpace(sid))
            return BadRequest(new { error = $"Missing {SessionHeader} header." });

        await _menu.ReleaseAsync(id, sid.ToString(), ct);
        return NoContent();
    }

    // --- Admin (requires an admin JWT issued by the User service) ---

    // GET /api/menu/admin/all — admin listing incl. unavailable items.
    [HttpGet("admin/all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken ct) =>
        Ok(await _menu.GetMenuAsync(availableOnly: false, ct));

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _menu.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuItemRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await _menu.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/availability")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetAvailability(int id, [FromBody] UpdateAvailabilityRequest request, CancellationToken ct)
    {
        var updated = await _menu.SetAvailabilityAsync(id, request.Availability, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _menu.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // --- Menu item pictures (stored as bytes in Postgres) ---

    // POST /api/menu/{id}/image — admin uploads the item's picture (multipart).
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "Image must be 10 MB or smaller." });

        var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { error = "Only PNG, JPEG or WebP images are allowed." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var updated = await _menu.SetImageAsync(id, ms.ToArray(), file.ContentType, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    // GET /api/menu/{id}/image — anonymous so <img> tags (no auth header) can load it.
    // The cache-busting ?v token on the URL keeps updated pictures fresh.
    [HttpGet("{id:int}/image")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(int id, CancellationToken ct)
    {
        var img = await _menu.GetImageAsync(id, ct);
        return img is null ? NotFound() : File(img.Value.Data, img.Value.ContentType);
    }
}
