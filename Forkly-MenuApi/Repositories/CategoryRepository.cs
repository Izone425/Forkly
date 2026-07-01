using Forkly.MenuService.Data;
using Forkly.MenuService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.MenuService.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly MenuDbContext _db;
    public CategoryRepository(MenuDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Categories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return category;
    }

    public Task UpdateAsync(Category category, CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Remove(category);
        return _db.SaveChangesAsync(ct);
    }

    public Task<bool> HasItemsAsync(int categoryId, CancellationToken ct = default) =>
        _db.MenuItems.AnyAsync(m => m.CategoryId == categoryId, ct);
}
