namespace OrderService.Menu;

// Menu data as the Order service needs it. Prices are in cents (RM * 100).
public record MenuItemDto(
    string Id,
    string Name,
    string Description,
    int PriceCents,
    string Category,
    string Emoji,
    bool Available);

// Abstraction over the menu source. Today: MockMenuClient.
// When amirul-menu ships its gRPC service: MenuGrpcClient (swap via config).
public interface IMenuClient
{
    Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(CancellationToken ct = default);
    Task<MenuItemDto?> GetItemAsync(string id, CancellationToken ct = default);
}
