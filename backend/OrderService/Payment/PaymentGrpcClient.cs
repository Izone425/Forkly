using Forkly.Contracts.Payment;

namespace OrderService.Payment;

// Real client for the payment team's gRPC service. Wraps the generated
// PaymentService.PaymentServiceClient. Not used until Payment:UseMock=false.
public sealed class PaymentGrpcClient : IPaymentClient
{
    private readonly PaymentService.PaymentServiceClient _client;

    public PaymentGrpcClient(PaymentService.PaymentServiceClient client) => _client = client;

    public async Task<PaymentResultDto> CreatePaymentAsync(
        string orderId,
        int amountCents,
        IReadOnlyList<PaymentLineDto> lines,
        CancellationToken ct = default)
    {
        var request = new CreatePaymentRequest
        {
            OrderId = orderId,
            AmountCents = amountCents,
            Currency = "MYR",
        };
        request.Lines.AddRange(lines.Select(l => new PaymentLine
        {
            Name = l.Name,
            UnitPriceCents = l.UnitPriceCents,
            Quantity = l.Quantity,
        }));

        var resp = await _client.CreatePaymentAsync(request, cancellationToken: ct);
        return new PaymentResultDto(resp.PaymentId, resp.RedirectUrl, resp.Status);
    }
}
