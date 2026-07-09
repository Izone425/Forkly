using System.Collections.Concurrent;
using Forkly.PaymentService.Models;

namespace Forkly.PaymentService.Services;

// In-memory payment store (singleton). This is a MOCK: payments are not persisted,
// so they reset when the service restarts. Swap for EF Core + a 'payment' schema
// later without changing the service/controller.
public interface IPaymentStore
{
    void Save(Payment payment);
    Payment? Get(string id);
    Payment? FindPendingByOrder(int orderId);
    Payment? LatestByOrder(int orderId);
}

public class PaymentStore : IPaymentStore
{
    private readonly ConcurrentDictionary<string, Payment> _byId = new();

    public void Save(Payment payment) => _byId[payment.Id] = payment;

    public Payment? Get(string id) => _byId.TryGetValue(id, out var p) ? p : null;

    public Payment? FindPendingByOrder(int orderId) =>
        _byId.Values.FirstOrDefault(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending);

    public Payment? LatestByOrder(int orderId) =>
        _byId.Values.Where(p => p.OrderId == orderId).OrderByDescending(p => p.CreatedAt).FirstOrDefault();
}
