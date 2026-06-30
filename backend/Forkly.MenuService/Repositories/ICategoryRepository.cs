using Forkly.MenuService.Models;

namespace Forkly.MenuService.Repositories;

// Data-access boundary for categories.
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Category> AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Category category, CancellationToken ct = default);
    Task<bool> HasItemsAsync(int categoryId, CancellationToken ct = default);
}
