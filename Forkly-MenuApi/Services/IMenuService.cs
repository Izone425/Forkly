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

    // Set a session's cart hold for an item to `quantity` (0 releases). The result says
    // whether it was accepted and how many units the session may still add.
    Task<ReservationResult> ReserveAsync(int id, string sessionId, int quantity, CancellationToken ct = default);

    // Drop a session's hold for an item.
    Task ReleaseAsync(int id, string sessionId, CancellationToken ct = default);

    // Finalise a checkout: atomically decrement stock for each line and clear the
    // session's holds. Returns whether it committed (and the short line if not).
    Task<Forkly.MenuService.Repositories.CommitOutcome> CommitStockAsync(
        string sessionId, IReadOnlyList<(int MenuId, int Quantity)> items, CancellationToken ct = default);
}

public enum ReservationStatus { Accepted, Insufficient, NotFound, Unavailable }

// Remaining = how many units the session may still add for the item.
public readonly record struct ReservationResult(ReservationStatus Status, int Remaining);
