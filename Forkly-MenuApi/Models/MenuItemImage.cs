namespace Forkly.MenuService.Models;

// A menu item's uploaded picture, stored as bytes in Postgres (bytea). Kept in its
// own 1:1 table so the (potentially large) image data is never pulled into the buyer
// or admin menu listings — only the dedicated image endpoint loads it.
public class MenuItemImage
{
    // PK and FK to MenuItems.Id (one image per menu item).
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public byte[] Data { get; set; } = [];          // bytea
    public string ContentType { get; set; } = "";   // image/png | image/jpeg | image/webp

    // Bumped on every upload; used as a cache-busting version token in the URL.
    public DateTimeOffset UpdatedAt { get; set; }
}
