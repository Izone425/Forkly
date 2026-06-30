using Forkly.MenuService.Data;
using Forkly.MenuService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.MenuService.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly MenuDbContext _db;
    public MenuRepository(MenuDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuItem>> GetAllAsync(bool availableOnly, CancellationToken ct = default)
    {
        var query = _db.MenuItems
            .Include(m => m.Category)
            .AsQueryable();

        if (availableOnly)
            query = query.Where(m => m.Availability);

        return await query
            .OrderBy(m => m.CategoryId)
            .ThenBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.MenuItems
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<MenuItem> AddAsync(MenuItem item, CancellationToken ct = default)
    {
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public Task UpdateAsync(MenuItem item, CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);  // EF tracks the loaded entity, just persist

    public Task DeleteAsync(MenuItem item, CancellationToken ct = default)
    {
        _db.MenuItems.Remove(item);
        return _db.SaveChangesAsync(ct);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct = default) =>
        _db.Categories.AnyAsync(c => c.Id == categoryId, ct);
}
