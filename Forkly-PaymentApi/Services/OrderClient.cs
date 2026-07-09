using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Forkly.PaymentService.Services;

// Minimal view of an order, as returned by the Order service (Hanif).
public record OrderInfo(int Id, string? Reference, int UserId, decimal Total, string Currency, string Status);

public interface IOrderClient
{
    // Reads the order (to get the authoritative amount). Forwards the caller's JWT,
    // so the Order service applies its own ownership checks. Null if not found/allowed.
    Task<OrderInfo?> GetOrderAsync(int orderId, string bearerToken, CancellationToken ct = default);

    // Marks the order's PAYMENT status Paid in the Order service (the single source
    // of truth). Kept separate from fulfilment status so it never overwrites
    // Preparing/Completed/etc. Returns false if the call failed.
    Task<bool> MarkPaidAsync(int orderId, string bearerToken, CancellationToken ct = default);
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
        return new OrderInfo(dto.Id, dto.Reference, dto.UserId, dto.Total, dto.Currency ?? "MYR", dto.Status ?? "");
    }

    public async Task<bool> MarkPaidAsync(int orderId, string bearerToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/orders/{orderId}/payment");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        req.Content = JsonContent.Create(new { paymentStatus = "Paid" });

        using var res = await _http.SendAsync(req, ct);
        return res.IsSuccessStatusCode;
    }

    private sealed class OrderDto
    {
        public int Id { get; set; }
        public string? Reference { get; set; }
        public int UserId { get; set; }
        public decimal Total { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
    }
}
