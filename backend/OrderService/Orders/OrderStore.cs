using System.Collections.Concurrent;

namespace OrderService.Orders;

// In-memory order storage for the training project. Swap for a database
// (EF Core / Dapper) later — the OrderManager is the only consumer.
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<string, OrderRecord> _orders = new();

    public void Save(OrderRecord order) => _orders[order.OrderId] = order;

    public OrderRecord? Get(string orderId) =>
        _orders.TryGetValue(orderId, out var order) ? order : null;
}
