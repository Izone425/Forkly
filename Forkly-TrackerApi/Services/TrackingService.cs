using Forkly.TrackerService.Dtos;

namespace Forkly.TrackerService.Services;

public interface ITrackingService
{
    // Null if the order doesn't exist or isn't the caller's.
    Task<TrackingResponse?> GetAsync(int orderId, string token, CancellationToken ct = default);
}

public class TrackingService : ITrackingService
{
    private const string Preparing = "Preparing";
    private const string Completed = "Completed";
    private const string OutForDelivery = "OutForDelivery";
    private const string Delivered = "Delivered";
    private const string Cancelled = "Cancelled";
    private const string PaidPayment = "Paid";

    // The customer-facing progress stages, in order.
    private static readonly (string Key, string Label)[] Timeline =
    {
        ("confirmed", "Order confirmed"),
        ("preparing", "Preparing"),
        ("ready", "Ready"),
        ("out", "Out for delivery"),
        ("delivered", "Delivered"),
    };

    private readonly IOrderClient _orders;
    private readonly int _deliverySeconds;

    public TrackingService(IOrderClient orders, IConfiguration config)
    {
        _orders = orders;
        _deliverySeconds = int.TryParse(config["Tracker:DeliverySeconds"], out var s) && s > 0 ? s : 90;
    }

    public async Task<TrackingResponse?> GetAsync(int orderId, string token, CancellationToken ct = default)
    {
        var order = await _orders.GetOrderAsync(orderId, token, ct);
        if (order is null) return null;

        var status = order.Status;
        var paid = string.Equals(order.PaymentStatus, PaidPayment, StringComparison.OrdinalIgnoreCase);

        int? etaMinutes = null, remainingSeconds = null;
        DateTimeOffset? deliveredAt = null;

        if (status == OutForDelivery)
        {
            // MOCK: a deterministic per-order delivery time (kept short so it's watchable
            // in a demo). Anchored to when the order went Out for delivery (UpdatedAt).
            var total = _deliverySeconds + (order.Id % 4) * 20;
            var etaTime = order.UpdatedAt.AddSeconds(total);
            var remaining = (int)Math.Round((etaTime - DateTimeOffset.UtcNow).TotalSeconds);

            if (remaining <= 0)
            {
                // Timer elapsed — Alia's tracker advances the order to Delivered.
                await _orders.MarkDeliveredAsync(orderId, token, ct);
                status = Delivered;
                deliveredAt = DateTimeOffset.UtcNow;
            }
            else
            {
                remainingSeconds = remaining;
                etaMinutes = (int)Math.Ceiling(remaining / 60.0);
            }
        }
        else if (status == Delivered)
        {
            deliveredAt = order.UpdatedAt;
        }

        return new TrackingResponse
        {
            OrderId = order.Id,
            Reference = order.Reference,
            Status = status,
            PaymentStatus = order.PaymentStatus,
            Total = order.Total,
            Message = MessageFor(status, paid, etaMinutes),
            EtaMinutes = etaMinutes,
            RemainingSeconds = remainingSeconds,
            DeliveredAt = deliveredAt,
            Steps = BuildSteps(status, paid),
            Items = order.Items.Select(i => new TrackingLine { ItemName = i.Name, Quantity = i.Quantity }).ToList(),
        };
    }

    // Fulfilment status -> index on the Timeline (payment drives step 0 separately).
    private static int IndexFor(string status) => status switch
    {
        Preparing => 1,
        Completed => 2,
        OutForDelivery => 3,
        Delivered => 4,
        _ => 0, // Pending / anything before the kitchen starts
    };

    private static List<TrackingStep> BuildSteps(string status, bool paid)
    {
        var current = IndexFor(status);
        var isDelivered = status == Delivered;
        var steps = new List<TrackingStep>(Timeline.Length);

        for (var i = 0; i < Timeline.Length; i++)
        {
            string state;
            if (i == 0) // "confirmed" is driven by payment, not fulfilment
                state = paid ? (current == 0 && !isDelivered ? "current" : "done") : "upcoming";
            else if (isDelivered) state = "done";
            else if (i < current) state = "done";
            else if (i == current) state = "current";
            else state = "upcoming";

            steps.Add(new TrackingStep { Key = Timeline[i].Key, Label = Timeline[i].Label, State = state });
        }
        return steps;
    }

    private static string MessageFor(string status, bool paid, int? etaMinutes) => status switch
    {
        Cancelled => "This order was cancelled.",
        Delivered => "Delivered — enjoy your meal! 🎉",
        OutForDelivery => etaMinutes is int m
            ? $"On its way 🛵 — arriving in about {m} min"
            : "On its way 🛵",
        Completed => "Your order is ready 🍽️",
        Preparing => "Your order is being prepared 👨‍🍳",
        _ => paid ? "Order confirmed — sending it to the kitchen." : "Waiting for payment…",
    };
}
