using Forkly.MenuService.Models;

namespace Forkly.MenuService.Repositories;

// Data-access boundary for menu items. Keeps EF Core out of the service layer.
public interface IMenuRepository
{
    Task<IReadOnlyList<MenuItem>> GetAllAsync(bool availableOnly, CancellationToken ct = default);
    Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MenuItem> AddAsync(MenuItem item, CancellationToken ct = default);
    Task UpdateAsync(MenuItem item, CancellationToken ct = default);
    Task DeleteAsync(MenuItem item, CancellationToken ct = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct = default);
}
