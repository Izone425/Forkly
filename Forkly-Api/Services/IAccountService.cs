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
}
