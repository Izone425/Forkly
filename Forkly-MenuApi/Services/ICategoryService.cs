using Forkly.MenuService.Dtos;

namespace Forkly.MenuService.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);
    // Returns false when not found; throws InvalidOperationException when the category still has items.
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
