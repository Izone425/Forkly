namespace Forkly.OrderService.Application.Exceptions;

// Thrown for invalid input (empty order, unknown menu item, invalid status).
// Surfaced by controllers as HTTP 400.
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
