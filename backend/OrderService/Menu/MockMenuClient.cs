namespace OrderService.Menu;

// Stand-in for the menu service until amirul-menu's gRPC endpoint is ready.
// Mirrors the frontend's static menu so the two stay consistent during dev.
// Swap to MenuGrpcClient by setting Menu:UseMock=false + Menu:GrpcAddress.
public sealed class MockMenuClient : IMenuClient
{
    private static readonly IReadOnlyList<MenuItemDto> Items = new List<MenuItemDto>
    {
        new("burger", "Classic Burger", "Beef patty, cheddar, lettuce & house sauce", 1000, "Mains", "🍔", true),
        new("wings",  "Chicken Wings",  "6 pcs, spicy honey glaze",                   1200, "Mains", "🍗", true),
        new("fries",  "Fries",          "Crispy golden, lightly salted",               500, "Sides", "🍟", true),
        new("sundae", "Ice Cream Sundae","Vanilla with chocolate drizzle",             700, "Desserts", "🍨", true),
        new("coffee", "Coffee",         "Freshly brewed, hot",                         600, "Drinks", "☕", true),
        new("soda",   "Soft Drink",     "Chilled, 500ml",                              400, "Drinks", "🥤", true),
    };

    public Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(CancellationToken ct = default)
        => Task.FromResult(Items);

    public Task<MenuItemDto?> GetItemAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
}
