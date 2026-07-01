namespace Forkly.Api.Models;

// A user's profile picture, stored as bytes in Postgres (bytea) rather than on
// disk. Kept in its own 1:1 table so the (potentially large) image data is never
// pulled into a normal Users query — only the dedicated avatar endpoints load it.
public class UserAvatar
{
    // PK and FK to Users.Id (one avatar per user).
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public byte[] Data { get; set; } = [];          // bytea
    public string ContentType { get; set; } = "";   // image/png | image/jpeg | image/webp

    // Bumped on every upload; used as a cache-busting version token in the URL.
    public DateTimeOffset UpdatedAt { get; set; }
}
