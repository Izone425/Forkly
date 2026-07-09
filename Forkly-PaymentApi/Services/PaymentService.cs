using Forkly.PaymentService.Dtos;
using Forkly.PaymentService.Models;

namespace Forkly.PaymentService.Services;

public interface IPaymentService
{
    // Start (or reuse) a payment for an order. Reads the amount from the Order
    // service; null if the order doesn't exist or isn't the caller's.
    Task<PaymentResponse?> CheckoutAsync(int orderId, int userId, string token, CancellationToken ct = default);

    // Settle the payment (mock). On success, marks the order Paid in the Order service.
    Task<PaymentResponse?> PayAsync(string paymentId, PayRequest request, string token, CancellationToken ct = default);

    PaymentResponse? Get(string paymentId);
    PaymentResponse? GetByOrder(int orderId);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentStore _store;
    private readonly IOrderClient _orders;

    public PaymentService(IPaymentStore store, IOrderClient orders)
    {
        _store = store;
        _orders = orders;
    }

    public async Task<PaymentResponse?> CheckoutAsync(int orderId, int userId, string token, CancellationToken ct = default)
    {
        var order = await _orders.GetOrderAsync(orderId, token, ct);
        if (order is null) return null;

        // If a pending payment already exists for this order, reuse it (idempotent checkout).
        var existing = _store.FindPendingByOrder(orderId);
        if (existing is not null) return Map(existing);

        var payment = new Payment
        {
            Id = "PAY-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            OrderId = order.Id,
            Reference = order.Reference,
            UserId = userId,
            Amount = order.Total,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _store.Save(payment);
        return Map(payment);
    }

    public async Task<PaymentResponse?> PayAsync(string paymentId, PayRequest request, string token, CancellationToken ct = default)
    {
        var payment = _store.Get(paymentId);
        if (payment is null) return null;
        if (payment.Status == PaymentStatus.Paid) return Map(payment); // idempotent

        payment.Method = request.Method;
        payment.CardLast4 = request.CardLast4;

        // Mock gateway: honour the failure test hook, otherwise "charge" succeeds.
        if (request.SimulateFailure)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Payment declined (simulated).";
            _store.Save(payment);
            return Map(payment);
        }

        // Confirm with the Order service FIRST — it's the single source of truth for
        // status. Only commit the payment as Paid if that succeeds; otherwise the
        // idempotency short-circuit above could lock a Paid payment against an order
        // stuck Pending, with no retry path. The kitchen (Zul) then sees the Paid
        // order by polling the Order service.
        bool marked;
        try
        {
            marked = await _orders.MarkPaidAsync(payment.OrderId, token, ct);
        }
        catch
        {
            marked = false; // order service unreachable — treat as not paid, allow retry
        }

        if (!marked)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Could not confirm the order as paid. Please try again.";
            _store.Save(payment);
            return Map(payment);
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTimeOffset.UtcNow;
        payment.FailureReason = null;
        _store.Save(payment);

        return Map(payment);
    }

    public PaymentResponse? Get(string paymentId)
    {
        var p = _store.Get(paymentId);
        return p is null ? null : Map(p);
    }

    public PaymentResponse? GetByOrder(int orderId)
    {
        var p = _store.LatestByOrder(orderId);
        return p is null ? null : Map(p);
    }

    private static PaymentResponse Map(Payment p) => new()
    {
        PaymentId = p.Id,
        OrderId = p.OrderId,
        Reference = p.Reference,
        Amount = p.Amount,
        Currency = p.Currency,
        Status = p.Status,
        Method = p.Method,
        CardLast4 = p.CardLast4,
        FailureReason = p.FailureReason,
        CreatedAt = p.CreatedAt,
        PaidAt = p.PaidAt,
    };
}
