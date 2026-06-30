namespace Forkly.OrderService.Menu;

// Bound from the "Menu" config section. UseMock (or an empty GrpcAddress) selects
// the offline MockMenuCatalog; otherwise the real gRPC client is used.
public class MenuOptions
{
    public bool UseMock { get; set; }
    public string GrpcAddress { get; set; } = string.Empty;
}
