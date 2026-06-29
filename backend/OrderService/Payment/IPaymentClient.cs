namespace OrderService.Payment;

public record PaymentLineDto(string Name, int UnitPriceCents, int Quantity);

public record PaymentResultDto(string PaymentId, string RedirectUrl, string Status);

// Abstraction over the payment service. On order confirmation, Order calls
// CreatePayment and hands the redirect URL back to the frontend.
// Today: MockPaymentClient. Later: PaymentGrpcClient (swap via config).
public interface IPaymentClient
{
    Task<PaymentResultDto> CreatePaymentAsync(
        string orderId,
        int amountCents,
        IReadOnlyList<PaymentLineDto> lines,
        CancellationToken ct = default);
}
