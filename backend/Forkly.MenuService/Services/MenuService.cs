using Forkly.MenuService.Dtos;
using Forkly.MenuService.Models;
using Forkly.MenuService.Repositories;

namespace Forkly.MenuService.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _repo;
    public MenuService(IMenuRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<MenuItemResponse>> GetMenuAsync(bool availableOnly, CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(availableOnly, ct);
        return items.Select(Map).ToList();
    }

    public async Task<MenuItemResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        return item is null ? null : Map(item);
    }

    public async Task<MenuItemResponse> CreateAsync(CreateMenuItemRequest request, CancellationToken ct = default)
    {
        if (!await _repo.CategoryExistsAsync(request.CategoryId, ct))
            throw new ArgumentException($"Category {request.CategoryId} does not exist.");

        var now = DateTimeOffset.UtcNow;
        var item = new MenuItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.UnitPrice,            // unitPrice (API) -> Price (entity/db)
            ImageUrl = request.ImageUrl.Trim(),
            StockQuantity = request.StockQuantity,
            Availability = request.Availability,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var saved = await _repo.AddAsync(item, ct);

        // Re-read so the Category navigation is populated for the response.
        var withCategory = await _repo.GetByIdAsync(saved.Id, ct);
        return Map(withCategory ?? saved);
    }

    public async Task<MenuItemResponse?> UpdateAsync(int id, UpdateMenuItemRequest request, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item is null) return null;

        if (!await _repo.CategoryExistsAsync(request.CategoryId, ct))
            throw new ArgumentException($"Category {request.CategoryId} does not exist.");

        item.CategoryId = request.CategoryId;
        item.Name = request.Name.Trim();
        item.Description = request.Description.Trim();
        item.Price = request.UnitPrice;
        item.ImageUrl = request.ImageUrl.Trim();
        item.StockQuantity = request.StockQuantity;
        item.Availability = request.Availability;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(item, ct);

        var refreshed = await _repo.GetByIdAsync(id, ct);
        return Map(refreshed ?? item);
    }

    public async Task<MenuItemResponse?> SetAvailabilityAsync(int id, bool availability, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item is null) return null;

        item.Availability = availability;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(item, ct);

        return Map(item);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item is null) return false;

        await _repo.DeleteAsync(item, ct);
        return true;
    }

    // Entity -> DTO. Note Price is surfaced as UnitPrice ("unitPrice" in JSON).
    private static MenuItemResponse Map(MenuItem m) => new()
    {
        Id = m.Id,
        CategoryId = m.CategoryId,
        Category = m.Category?.Name ?? string.Empty,
        Name = m.Name,
        Description = m.Description,
        UnitPrice = m.Price,
        ImageUrl = m.ImageUrl,
        StockQuantity = m.StockQuantity,
        Availability = m.Availability,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
    };
}
