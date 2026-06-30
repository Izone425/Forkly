namespace Forkly.OrderService.Menu;

public enum MenuCatalogMode { Mock, Grpc }

// Bound from the "Menu" config section.
public class MenuOptions
{
    public bool UseMock { get; set; }
    public string GrpcAddress { get; set; } = string.Empty;

    // Decide which IMenuCatalog to wire. Fails CLOSED: the offline mock is used only
    // when explicitly opted into via UseMock=true. If UseMock is false and no
    // GrpcAddress is configured we throw rather than silently falling back to the
    // non-authoritative mock (which would price every order at RM0 and accept any item).
    public MenuCatalogMode ResolveMode()
    {
        if (UseMock)
            return MenuCatalogMode.Mock;
        if (string.IsNullOrWhiteSpace(GrpcAddress))
            throw new InvalidOperationException(
                "Menu:GrpcAddress must be configured when Menu:UseMock is false. " +
                "Refusing to fall back to the non-authoritative mock catalog.");
        return MenuCatalogMode.Grpc;
    }
}
