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
}
