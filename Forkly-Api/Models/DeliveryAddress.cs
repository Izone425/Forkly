namespace Forkly.Api.Models;

// A delivery address belonging to a user. A user may have many; exactly one is
// flagged IsDefault (enforced in AccountService). Replaces the old single set of
// flat address columns that used to live on ApplicationUser.
public class DeliveryAddress
{
    public int Id { get; set; }

    // FK → Users.Id (cascade delete configured in AppDbContext).
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Optional friendly name, e.g. "Home", "Office".
    public string? Label { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string? DeliveryNotes { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
