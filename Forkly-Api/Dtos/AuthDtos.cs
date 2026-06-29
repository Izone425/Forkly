using System.ComponentModel.DataAnnotations;

namespace Forkly.Api.Dtos;

// ---- Requests (REST body shapes; the gRPC equivalents live in auth.proto) ----

public class RegisterRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }
}

// Create/update body for a delivery address. Delivery addresses are managed as a
// separate sub-resource under /api/auth/me/addresses.
public class AddressRequest
{
    public string? Label { get; set; }

    [Required]
    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string? DeliveryNotes { get; set; }

    // When true, this address becomes the default (the previous default is cleared).
    public bool IsDefault { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ---- Responses ----

public class DeliveryAddressDto
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string? DeliveryNotes { get; set; }
    public bool IsDefault { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();

    // Profile details.
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }

    // All delivery addresses, plus the id of the default one.
    public List<DeliveryAddressDto> Addresses { get; set; } = new();
    public int? DefaultAddressId { get; set; }

    // Convenience mirror of the default address (kept so existing clients that read
    // a single address — e.g. the landing page hero — keep working unchanged).
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string? DeliveryNotes { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}
