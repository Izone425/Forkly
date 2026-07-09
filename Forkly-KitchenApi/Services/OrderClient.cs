using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Forkly.KitchenService.Dtos;

namespace Forkly.KitchenService.Services;

// Talks to the Order service (the single source of truth for orders + status).
// The kitchen holds no data of its own — it reads the queue and relays crew
// status changes. The caller's JWT is forwarded so the Order service authorises.
public interface IOrderClient
{
    Task<IReadOnlyList<KitchenTicket>> GetQueueAsync(string bearerToken, CancellationToken ct = default);
    Task<KitchenTicket?> UpdateStatusAsync(int orderId, string status, string bearerToken, CancellationToken ct = default);
}

public class OrderClient : IOrderClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public OrderClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<KitchenTicket>> GetQueueAsync(string bearerToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/orders/kitchen/queue");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) return Array.Empty<KitchenTicket>();

        var orders = await res.Content.ReadFromJsonAsync<List<OrderDto>>(Json, ct) ?? new();
        return orders.Select(Map).ToList();
    }

    public async Task<KitchenTicket?> UpdateStatusAsync(int orderId, string status, string bearerToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/orders/{orderId}/status");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        req.Content = JsonContent.Create(new { status });

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) return null;

        var dto = await res.Content.ReadFromJsonAsync<OrderDto>(Json, ct);
        return dto is null ? null : Map(dto);
    }

    private static KitchenTicket Map(OrderDto o) => new()
    {
        OrderId = o.Id,
        Reference = o.Reference,
        Status = o.Status ?? string.Empty,
        PlacedAt = o.CreatedAt,
        Total = o.Total,
        Items = (o.Items ?? new()).Select(i => new KitchenLine
        {
            ItemName = i.ItemName ?? string.Empty,
            Quantity = i.Quantity,
        }).ToList(),
    };

    private sealed class OrderDto
    {
        public int Id { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public decimal Total { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<ItemDto>? Items { get; set; }
    }

    private sealed class ItemDto
    {
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
