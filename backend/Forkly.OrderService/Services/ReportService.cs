using Forkly.OrderService.Dtos;
using Forkly.OrderService.Repositories;

namespace Forkly.OrderService.Services;

public class ReportService : IReportService
{
    private static readonly string[] MonthLabels =
        { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private readonly IOrderRepository _repo;

    public ReportService(IOrderRepository repo) => _repo = repo;

    public async Task<MonthlySalesReportResponse> GetMonthlySalesAsync(CancellationToken ct = default)
    {
        // All non-cancelled orders count as booked sales. (Tighten to Paid+ later
        // if the report should exclude orders that were created but never paid.)
        var orders = await _repo.GetAllForReportAsync(ct);

        var months = orders
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlySalesResponse
            {
                Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                Label = MonthLabels[g.Key.Month],
                OrderCount = g.Count(),
                Total = g.Sum(o => o.Total),
                Items = g
                    .SelectMany(o => o.Items)
                    .GroupBy(i => i.ItemName)
                    .Select(ig => new ItemSalesResponse
                    {
                        Name = ig.Key,
                        Qty = ig.Sum(i => i.Quantity),
                        Sales = ig.Sum(i => i.Price * i.Quantity),
                    })
                    .OrderByDescending(i => i.Sales)
                    .ToList(),
            })
            .ToList();

        return new MonthlySalesReportResponse
        {
            Currency = "MYR",
            GeneratedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Months = months,
        };
    }
}
