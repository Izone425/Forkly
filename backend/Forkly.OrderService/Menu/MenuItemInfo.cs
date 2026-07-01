namespace Forkly.OrderService.Menu;

// Authoritative menu facts the Order service needs to price/validate a cart line.
// Price is in RM (decimal), converted from the contract's price_cents.
public record MenuItemInfo(int Id, string Name, decimal Price, bool Available);
