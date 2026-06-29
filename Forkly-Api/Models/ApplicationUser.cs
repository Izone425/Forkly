using Microsoft.AspNetCore.Identity;

namespace Forkly.Api.Models;

// The Forkly user. Extends IdentityUser, which already provides Id, UserName,
// Email, PasswordHash, security stamp, lockout, etc. We only add the profile
// fields the registration form collects.
public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Profile picture — relative path served from wwwroot (e.g. /uploads/{id}.png).
    public string? AvatarUrl { get; set; }

    // Delivery addresses (one-to-many). A user may have several; one is the default.
    // (PhoneNumber is inherited from IdentityUser.)
    public ICollection<DeliveryAddress> Addresses { get; set; } = new List<DeliveryAddress>();
}
