using Forkly.Api.Dtos;

namespace Forkly.Api.Services;

// The single source of truth for account operations, shared by the REST and
// gRPC facades.
public interface IAccountService
{
    Task<AccountResult> RegisterAsync(RegisterRequest request);

    Task<AccountResult> LoginAsync(LoginRequest request);

    // Builds a UserDto for an already-authenticated user id (from a validated
    // JWT). Returns null if the user no longer exists.
    Task<UserDto?> GetUserAsync(string userId);

    // Update editable profile fields (name, phone). Delivery addresses are managed
    // separately. Returns the updated UserDto, or null if the user no longer exists.
    Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileRequest request);

    // ---- Delivery addresses ----
    // All address mutations return the full updated UserDto (so callers can refresh
    // the user in one round-trip), or null if the user / address doesn't exist.
    Task<UserDto?> AddAddressAsync(string userId, AddressRequest request);
    Task<UserDto?> UpdateAddressAsync(string userId, int addressId, AddressRequest request);
    Task<UserDto?> DeleteAddressAsync(string userId, int addressId);
    Task<UserDto?> SetDefaultAddressAsync(string userId, int addressId);

    // Set the avatar URL (relative path) after a file upload.
    Task<UserDto?> SetAvatarAsync(string userId, string avatarUrl);

    // Change the password (requires the correct current password).
    Task<AccountResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    // ---- Admin user management (admin-only callers; the controller enforces the role) ----

    // Paged list of all users with roles + lockout state. Optional case-insensitive
    // search over email / full name.
    Task<PagedResult<AdminUserListItemDto>> ListUsersAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    // Full detail for one user (profile + admin flags). Null if the user is gone.
    Task<AdminUserDetailDto?> GetUserDetailAsync(string userId);

    // Add/remove the "admin" role. Refuses to remove the last admin or the caller's
    // own admin role. actingUserId is the id from the caller's JWT.
    Task<AdminActionResult> SetAdminRoleAsync(string userId, bool makeAdmin, string actingUserId);

    // Disable (permanent lockout) / enable (clear lockout) an account. Refuses to
    // disable the caller's own account.
    Task<AdminActionResult> SetUserDisabledAsync(string userId, bool disabled, string actingUserId);
}
