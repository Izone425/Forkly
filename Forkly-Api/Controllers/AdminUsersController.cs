using System.Security.Claims;
using Forkly.Api.Models;
using Forkly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.Api.Controllers;

// Admin-only user management. Thin facade over IAccountService, mirroring
// AuthController. The role string is lowercase "admin" (Roles.Admin) — it must
// match the role seeded in DbSeeder and the ClaimTypes.Role claim TokenService
// emits, or this guard silently rejects every admin.
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly IAccountService _accounts;

    public AdminUsersController(IAccountService accounts) => _accounts = accounts;

    // GET /api/admin/users?search=&page=&pageSize= — paged list with roles + status.
    [HttpGet]
    public async Task<IActionResult> List(string? search, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _accounts.ListUsersAsync(search, page, pageSize, ct));

    // GET /api/admin/users/{id} — single user detail.
    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        var detail = await _accounts.GetUserDetailAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    // POST /api/admin/users/{id}/roles/admin — promote to admin (idempotent).
    [HttpPost("{id}/roles/admin")]
    public Task<IActionResult> Promote(string id) => SetAdmin(id, makeAdmin: true);

    // DELETE /api/admin/users/{id}/roles/admin — demote from admin.
    [HttpDelete("{id}/roles/admin")]
    public Task<IActionResult> Demote(string id) => SetAdmin(id, makeAdmin: false);

    // POST /api/admin/users/{id}/disable — lock the account out of login.
    [HttpPost("{id}/disable")]
    public Task<IActionResult> Disable(string id) => SetDisabled(id, disabled: true);

    // POST /api/admin/users/{id}/enable — restore login access.
    [HttpPost("{id}/enable")]
    public Task<IActionResult> Enable(string id) => SetDisabled(id, disabled: false);

    private async Task<IActionResult> SetAdmin(string id, bool makeAdmin) =>
        ToResponse(await _accounts.SetAdminRoleAsync(id, makeAdmin, CurrentUserId() ?? string.Empty));

    private async Task<IActionResult> SetDisabled(string id, bool disabled) =>
        ToResponse(await _accounts.SetUserDisabledAsync(id, disabled, CurrentUserId() ?? string.Empty));

    // The User service signs the user id as the standard NameIdentifier ("sub") claim.
    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private IActionResult ToResponse(AdminActionResult result)
    {
        if (result.Succeeded) return Ok(result.User);

        var problem = new { error = result.Message };
        return result.Error switch
        {
            AdminActionError.NotFound => NotFound(problem),
            _ => Conflict(problem), // LastAdmin / SelfAction — business-rule conflicts
        };
    }
}
