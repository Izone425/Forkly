using Forkly.MenuService.Dtos;
using Forkly.MenuService.Models;
using Forkly.MenuService.Repositories;

namespace Forkly.MenuService.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _repo.GetAllAsync(ct);
        return categories.Select(Map).ToList();
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct);
        return category is null ? null : Map(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var saved = await _repo.AddAsync(category, ct);
        return Map(saved);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct);
        if (category is null) return null;

        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(category, ct);

        return Map(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct);
        if (category is null) return false;

        if (await _repo.HasItemsAsync(id, ct))
            throw new InvalidOperationException("Cannot delete a category that still has menu items.");

        await _repo.DeleteAsync(category, ct);
        return true;
    }

    private static CategoryResponse Map(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}
