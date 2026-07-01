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

        var items = await query
            .OrderBy(m => m.CategoryId)
            .ThenBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        await PopulateImageMetaAsync(items, ct);
        return items;
    }

    public async Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.MenuItems
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (item is not null)
            await PopulateImageMetaAsync([item], ct);
        return item;
    }

    // Fills HasImage/ImageUpdatedAt from a single byte-free query so listings can
    // resolve the served image URL without ever loading image bytes.
    private async Task PopulateImageMetaAsync(IReadOnlyList<MenuItem> items, CancellationToken ct)
    {
        if (items.Count == 0) return;

        var ids = items.Select(i => i.Id).ToList();
        var meta = await _db.MenuItemImages
            .Where(x => ids.Contains(x.MenuItemId))
            .Select(x => new { x.MenuItemId, x.UpdatedAt })
            .ToDictionaryAsync(x => x.MenuItemId, x => x.UpdatedAt, ct);

        foreach (var item in items)
            if (meta.TryGetValue(item.Id, out var updatedAt))
            {
                item.HasImage = true;
                item.ImageUpdatedAt = updatedAt;
            }
    }

    public async Task SetImageAsync(int menuItemId, byte[] data, string contentType, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        var image = await _db.MenuItemImages.FirstOrDefaultAsync(x => x.MenuItemId == menuItemId, ct);
        if (image is null)
        {
            image = new MenuItemImage { MenuItemId = menuItemId };
            _db.MenuItemImages.Add(image);
        }
        image.Data = data;
        image.ContentType = contentType;
        image.UpdatedAt = updatedAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(byte[] Data, string ContentType)?> GetImageAsync(int id, CancellationToken ct = default)
    {
        var image = await _db.MenuItemImages
            .Where(x => x.MenuItemId == id)
            .Select(x => new { x.Data, x.ContentType })
            .FirstOrDefaultAsync(ct);

        if (image is null || image.Data.Length == 0) return null;
        return (image.Data, string.IsNullOrEmpty(image.ContentType) ? "image/jpeg" : image.ContentType);
    }

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
