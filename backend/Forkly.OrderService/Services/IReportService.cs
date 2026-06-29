using Forkly.OrderService.Dtos;

namespace Forkly.OrderService.Services;

public interface IReportService
{
    // Sales aggregated by calendar month, each month broken down by menu item.
    Task<MonthlySalesReportResponse> GetMonthlySalesAsync(CancellationToken ct = default);
}
