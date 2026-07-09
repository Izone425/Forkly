using Forkly.KitchenService.Dtos;

namespace Forkly.KitchenService.Services;

public interface IKitchenService
{
    Task<IReadOnlyList<KitchenTicket>> GetQueueAsync(string token, CancellationToken ct = default);

    // Advance a ticket. Throws ArgumentException if the target isn't a status the
    // kitchen is allowed to set. Null if the order wasn't found.
    Task<KitchenTicket?> SetStatusAsync(int orderId, string status, string token, CancellationToken ct = default);
}

public class KitchenService : IKitchenService
{
    // The only status changes the kitchen may make. (Paid arrives from Payment;
    // Delivered is Alia's; Cancelled is Order/Payment.)
    private static readonly string[] Allowed = { "Preparing", "Completed", "OutForDelivery" };

    private readonly IOrderClient _orders;

    public KitchenService(IOrderClient orders) => _orders = orders;

    public Task<IReadOnlyList<KitchenTicket>> GetQueueAsync(string token, CancellationToken ct = default) =>
        _orders.GetQueueAsync(token, ct);

    public Task<KitchenTicket?> SetStatusAsync(int orderId, string status, string token, CancellationToken ct = default)
    {
        var canonical = Allowed.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
            throw new ArgumentException($"The kitchen can only set: {string.Join(", ", Allowed)}.");

        return _orders.UpdateStatusAsync(orderId, canonical, token, ct);
    }
}
