using OrderService.Menu;
using OrderService.Payment;

namespace OrderService.Orders;

// Core order workflow, shared by the REST API and the gRPC server:
//   1. price + validate each cart line against the menu service
//   2. compute subtotal, SST (6%), total
//   3. create a payment (payment service) and capture the redirect URL
//   4. persist and return the order
public sealed class OrderManager
{
    private const decimal SstRate = 0.06m; // 6% SST (Malaysia)

    private readonly IMenuClient _menu;
    private readonly IPaymentClient _payment;
    private readonly OrderStore _store;
    private readonly ILogger<OrderManager> _log;

    public OrderManager(IMenuClient menu, IPaymentClient payment, OrderStore store, ILogger<OrderManager> log)
    {
        _menu = menu;
        _payment = payment;
        _store = store;
        _log = log;
    }

    public OrderRecord? Get(string orderId) => _store.Get(orderId);

    public async Task<OrderRecord> CreateOrderAsync(
        IEnumerable<(string Id, int Quantity)> requestedItems,
        CancellationToken ct = default)
    {
        var lines = new List<OrderLine>();
        foreach (var (id, quantity) in requestedItems)
        {
            if (quantity <= 0) continue;

            var item = await _menu.GetItemAsync(id, ct)
                ?? throw new OrderValidationException($"Unknown menu item '{id}'.");
            if (!item.Available)
                throw new OrderValidationException($"'{item.Name}' is currently unavailable.");

            lines.Add(new OrderLine(item.Id, item.Name, item.PriceCents, quantity));
        }

        if (lines.Count == 0)
            throw new OrderValidationException("Order must contain at least one item.");

        var subtotal = lines.Sum(l => l.LineTotalCents);
        var tax = (int)Math.Round(subtotal * SstRate, MidpointRounding.AwayFromZero);
        var total = subtotal + tax;

        var orderId = "ORD-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var order = new OrderRecord
        {
            OrderId = orderId,
            Items = lines,
            SubtotalCents = subtotal,
            TaxCents = tax,
            TotalCents = total,
            Status = "AWAITING_PAYMENT",
        };

        // Backend-to-backend handoff: create the payment, keep the redirect URL.
        var payLines = lines
            .Select(l => new PaymentLineDto(l.Name, l.UnitPriceCents, l.Quantity))
            .ToList();
        var payment = await _payment.CreatePaymentAsync(orderId, total, payLines, ct);
        order.PaymentId = payment.PaymentId;
        order.PaymentRedirectUrl = payment.RedirectUrl;

        _store.Save(order);
        _log.LogInformation(
            "Created order {OrderId}: {LineCount} lines, total {TotalCents} cents, payment {PaymentId}",
            orderId, lines.Count, total, payment.PaymentId);

        return order;
    }
}
