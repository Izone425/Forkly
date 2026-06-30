namespace Forkly.OrderService.Menu;

// Thrown when the Menu service cannot be reached (as opposed to the item simply
// not existing). The controller maps this to HTTP 503.
public class MenuUnavailableException : Exception
{
    public MenuUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
