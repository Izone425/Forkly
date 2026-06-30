namespace Forkly.MenuService.Models;

// A menu category (Burger, Pasta, Steak, ...). Owned entirely by the Menu Service
// and stored in its own isolated "menu" schema in the shared foodorder database.
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
