using Forkly.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.OrderService.Controllers;

// Admin sales reporting, derived from order data. Consumed by the admin report
// page (Izzuwan's dashboard routes admins here).
[ApiController]
[Route("api/orders/reports")]
// Sales figures are admin-only. The role string is lowercase "admin" to match the
// ClaimTypes.Role claim the User service (Forkly-Api/TokenService) signs into the JWT.
[Authorize(Roles = "admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    // GET /api/orders/reports/monthly — total sales per month + per-menu-item breakdown.
    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(CancellationToken ct) =>
        Ok(await _reports.GetMonthlySalesAsync(ct));
}
