using Forkly.Contracts.Order;
using Grpc.Core;

namespace OrderService.Orders;

// gRPC server implementation of order.proto, so OTHER backends (payment,
// kitchen, etc.) can read/create orders backend-to-backend. The browser uses
// the REST API instead. Both paths share OrderManager.
public sealed class OrderGrpcService : Forkly.Contracts.Order.OrderService.OrderServiceBase
{
    private readonly OrderManager _manager;

    public OrderGrpcService(OrderManager manager) => _manager = manager;

    public override async Task<Order> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        try
        {
            var record = await _manager.CreateOrderAsync(
                request.Items.Select(i => (i.Id, i.Quantity)), context.CancellationToken);
            return ToProto(record);
        }
        catch (OrderValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override Task<Order> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        var record = _manager.Get(request.OrderId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Order '{request.OrderId}' not found."));
        return Task.FromResult(ToProto(record));
    }

    private static Order ToProto(OrderRecord r)
    {
        var order = new Order
        {
            OrderId = r.OrderId,
            SubtotalCents = r.SubtotalCents,
            TaxCents = r.TaxCents,
            TotalCents = r.TotalCents,
            Status = r.Status,
            PaymentId = r.PaymentId ?? "",
            PaymentRedirectUrl = r.PaymentRedirectUrl ?? "",
        };
        order.Items.AddRange(r.Items.Select(l => new Forkly.Contracts.Order.OrderItem
        {
            Id = l.Id,
            Name = l.Name,
            UnitPriceCents = l.UnitPriceCents,
            Quantity = l.Quantity,
        }));
        return order;
    }
}
