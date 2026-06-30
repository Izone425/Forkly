namespace Forkly.OrderService.Dtos;

// Monthly sales report shapes. Matches the contract the admin report page consumes
// (frontend src/services/reportApi.js → GET /api/orders/reports/monthly).

public class MonthlySalesReportResponse
{
    public string Currency { get; set; } = "MYR";
    public string GeneratedAt { get; set; } = string.Empty; // yyyy-MM-dd
    public List<MonthlySalesResponse> Months { get; set; } = new();
}

public class MonthlySalesResponse
{
    public string Month { get; set; } = string.Empty; // yyyy-MM
    public string Label { get; set; } = string.Empty; // Jan, Feb, ...
    public int OrderCount { get; set; }
    public decimal Total { get; set; }
    public List<ItemSalesResponse> Items { get; set; } = new();
}

public class ItemSalesResponse
{
    public string Name { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Sales { get; set; }
}
