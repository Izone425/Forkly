using System.Security.Claims;
using Forkly.Api.Data;
using Forkly.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forkly.Api.Controllers;

// Read-only orders facade for the account "Order history" view. The separate
// ordering module owns order creation and status transitions (writing into the
// same Orders/OrderItems tables); this controller only reads the current user's
// orders. That module can extend this controller with POST/PATCH endpoints.
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/orders → the signed-in user's orders, newest first, with line items.
    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        if (!int.TryParse(CurrentUserId(), out var userId)) return Unauthorized();

        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                Reference = o.Reference,
                Status = o.Status,
                PlacedAt = o.PlacedAt,
                Total = o.Total,
                Currency = o.Currency,
                Items = o.Items
                    .Select(i => new OrderItemDto
                    {
                        Name = i.Name,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                    })
                    .ToList(),
            })
            .ToListAsync();

        return Ok(orders);
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
