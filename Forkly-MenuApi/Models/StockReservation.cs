namespace Forkly.MenuService.Models;

// A short-lived hold placed on a menu item's stock while it sits in a shopper's cart.
// Available stock for buyers = MenuItem.StockQuantity minus the SUM of every session's
// active (non-expired) reservation for that item — so two shoppers can't be sold the
// same units. There is at most one row per (MenuItemId, SessionId): a session's hold for
// an item is an ABSOLUTE quantity that is upserted as the cart line changes, and deleted
// when the line is removed. Rows auto-expire via ExpiresAt (refreshed on each cart change)
// so abandoned carts release their stock.
public class StockReservation
{
    public int Id { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    // Guest/session identifier sent by the browser (X-Forkly-Session header).
    public string SessionId { get; set; } = string.Empty;

    public int Quantity { get; set; }

    // The hold lapses at this instant; reads only count rows where ExpiresAt > now.
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
