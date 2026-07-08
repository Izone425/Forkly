using System.Security.Claims;
using Forkly.PaymentService.Dtos;
using Forkly.PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.PaymentService.Controllers;

// Mock payment endpoints. Every call requires a valid JWT (the same one the User
// service issues). The caller's token is forwarded to the Order service so it
// applies its own ownership checks and status update.
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments) => _payments = payments;

    // POST /api/payments/checkout { orderId } — start a payment for an existing order.
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (!TryAuth(out var userId, out var token)) return Unauthorized();

        var payment = await _payments.CheckoutAsync(request.OrderId, userId, token, ct);
        return payment is null
            ? NotFound(new { error = "Order not found or not yours." })
            : Ok(payment);
    }

    // POST /api/payments/{paymentId}/pay — settle the payment (mock gateway).
    [HttpPost("{paymentId}/pay")]
    public async Task<IActionResult> Pay(string paymentId, [FromBody] PayRequest request, CancellationToken ct)
    {
        if (!TryAuth(out _, out var token)) return Unauthorized();

        var payment = await _payments.PayAsync(paymentId, request, token, ct);
        return payment is null ? NotFound() : Ok(payment);
    }

    // GET /api/payments/{paymentId} — current status of a payment.
    [HttpGet("{paymentId}")]
    public IActionResult Get(string paymentId)
    {
        var payment = _payments.Get(paymentId);
        return payment is null ? NotFound() : Ok(payment);
    }

    // GET /api/payments/order/{orderId} — latest payment for an order (others can check).
    [HttpGet("order/{orderId:int}")]
    public IActionResult GetByOrder(int orderId)
    {
        var payment = _payments.GetByOrder(orderId);
        return payment is null ? NotFound() : Ok(payment);
    }

    // User id from the JWT + the raw bearer token to forward to the Order service.
    private bool TryAuth(out int userId, out string token)
    {
        token = string.Empty;
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(raw, out userId)) return false;

        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = auth["Bearer ".Length..].Trim();
        return token.Length > 0;
    }
}
