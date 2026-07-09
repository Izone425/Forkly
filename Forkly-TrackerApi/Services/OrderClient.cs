using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Forkly.TrackerService.Services;

public record OrderLine(string Name, int Quantity);
public record OrderInfo(
    int Id, string? Reference, string Status, string PaymentStatus,
    decimal Total, DateTimeOffset UpdatedAt, IReadOnlyList<OrderLine> Items);

// Reads the order (source of truth for status) and, when the mock delivery timer
// elapses, advances it to Delivered. The caller's JWT is forwarded so the Order
// service applies its own ownership check.
public interface IOrderClient
{
    Task<OrderInfo?> GetOrderAsync(int orderId, string bearerToken, CancellationToken ct = default);
    Task<bool> MarkDeliveredAsync(int orderId, string bearerToken, CancellationToken ct = default);
}

public class OrderClient : IOrderClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public OrderClient(HttpClient http) => _http = http;

    public async Task<OrderInfo?> GetOrderAsync(int orderId, string bearerToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{orderId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) return null;

        var dto = await res.Content.ReadFromJsonAsync<OrderDto>(Json, ct);
        if (dto is null) return null;

        return new OrderInfo(
            dto.Id, dto.Reference, dto.Status ?? string.Empty, dto.PaymentStatus ?? string.Empty,
            dto.Total, dto.UpdatedAt,
            (dto.Items ?? new()).Select(i => new OrderLine(i.ItemName ?? string.Empty, i.Quantity)).ToList());
    }

    public async Task<bool> MarkDeliveredAsync(int orderId, string bearerToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/orders/{orderId}/status");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        req.Content = JsonContent.Create(new { status = "Delivered" });

        using var res = await _http.SendAsync(req, ct);
        return res.IsSuccessStatusCode;
    }

    private sealed class OrderDto
    {
        public int Id { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal Total { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<ItemDto>? Items { get; set; }
    }

    private sealed class ItemDto
    {
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
