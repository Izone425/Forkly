using Forkly.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.OrderService.Controllers;

// Admin sales reporting, derived from order data. Consumed by the admin report
// page (Izzuwan's dashboard routes admins here).
[ApiController]
[Route("api/orders/reports")]
[Authorize] // TODO: tighten to [Authorize(Roles = "Admin")] once the User service's
            // role claim type is confirmed, so only admins can pull sales figures.
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    // GET /api/orders/reports/monthly — total sales per month + per-menu-item breakdown.
    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(CancellationToken ct) =>
        Ok(await _reports.GetMonthlySalesAsync(ct));
}
