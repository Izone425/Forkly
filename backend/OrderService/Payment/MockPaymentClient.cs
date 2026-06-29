using Microsoft.Extensions.Options;
using OrderService.Configuration;

namespace OrderService.Payment;

// Stand-in for the payment service (other PIC, not ready). Produces a fake
// payment id and a redirect URL pointing at the payment page, so the
// confirm -> pay handoff can be demonstrated end-to-end today.
public sealed class MockPaymentClient : IPaymentClient
{
    private readonly PaymentOptions _options;

    public MockPaymentClient(IOptions<PaymentOptions> options) => _options = options.Value;

    public Task<PaymentResultDto> CreatePaymentAsync(
        string orderId,
        int amountCents,
        IReadOnlyList<PaymentLineDto> lines,
        CancellationToken ct = default)
    {
        var paymentId = "PAY-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        // Where the frontend should send the customer to pay.
        var baseUrl = _options.PaymentPageUrl.TrimEnd('/');
        var redirectUrl = $"{baseUrl}?pid={paymentId}&orderId={orderId}";

        return Task.FromResult(new PaymentResultDto(paymentId, redirectUrl, "PENDING"));
    }
}
