using Forkly.MenuService.Dtos;

namespace Forkly.MenuService.Services;

// Business logic boundary. Controllers depend on this, never on EF entities.
public interface IMenuService
{
    // availableOnly=true powers the public buyer listing; admins can see everything.
    Task<IReadOnlyList<MenuItemResponse>> GetMenuAsync(bool availableOnly, CancellationToken ct = default);
    Task<MenuItemResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MenuItemResponse> CreateAsync(CreateMenuItemRequest request, CancellationToken ct = default);
    Task<MenuItemResponse?> UpdateAsync(int id, UpdateMenuItemRequest request, CancellationToken ct = default);
    Task<MenuItemResponse?> SetAvailabilityAsync(int id, bool availability, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    // Store an uploaded picture's bytes for a menu item. Returns the updated DTO, or
    // null if the item doesn't exist.
    Task<MenuItemResponse?> SetImageAsync(int id, byte[] data, string contentType, CancellationToken ct = default);

    // Read a menu item's stored picture bytes + content type, or null if none.
    Task<(byte[] Data, string ContentType)?> GetImageAsync(int id, CancellationToken ct = default);
}
