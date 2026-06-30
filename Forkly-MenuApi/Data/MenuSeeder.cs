using Forkly.MenuService.Models;
using Microsoft.EntityFrameworkCore;

namespace Forkly.MenuService.Data;

// Seeds the Menu Service with a realistic Malaysia-style Halal Western menu so a
// fresh clone returns real data from GET /api/menu immediately. Idempotent: it only
// inserts when the tables are empty, so restarting the service never duplicates rows.
//
// Image URLs are high-resolution Unsplash CDN links (?w=1200&q=80) suitable for HD
// menu cards in the frontend. We store the URL only — there is no upload system.
public static class MenuSeeder
{
    private const string Img = "?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80";

    public static async Task SeedAsync(MenuDbContext db, CancellationToken ct = default)
    {
        if (await db.MenuItems.AnyAsync(ct))
            return;  // already seeded

        var now = DateTimeOffset.UtcNow;

        // --- Categories ---
        var categories = new[]
        {
            new Category { Name = "Burger",   Description = "Flame-grilled halal beef & chicken burgers", CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Pasta",    Description = "Italian-style pastas, made fresh",            CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Steak",    Description = "Premium halal cuts, grilled to order",        CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Chicken",  Description = "Western chicken favourites",                  CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Pizza",    Description = "Hand-tossed wood-fired pizzas",               CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Seafood",  Description = "Fresh catch, Western style",                  CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Dessert",  Description = "Sweet treats to finish",                      CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Beverage", Description = "Drinks, mocktails & coffee",                  CreatedAt = now, UpdatedAt = now },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(ct);

        Category Cat(string name) => categories.First(c => c.Name == name);

        // --- Menu items ---
        var items = new List<MenuItem>
        {
            // Burger
            Item(Cat("Burger"),  "Classic Beef Burger",  "Grilled halal beef patty, cheddar, caramelised onions & house sauce", 18.90m, "photo-1568901346375-23c9450c58cd", 40, now),
            Item(Cat("Burger"),  "Double Cheese Burger", "Two beef patties, double cheddar, pickles & smoky mayo",              24.90m, "photo-1586190848861-99aa4a171e90", 30, now),
            Item(Cat("Burger"),  "Crispy Chicken Burger","Buttermilk-fried chicken thigh, slaw & spicy aioli",                  16.90m, "photo-1606755962773-d324e0a13086", 35, now),

            // Pasta
            Item(Cat("Pasta"),   "Carbonara Pasta",      "Creamy egg & smoked beef bacon spaghetti with parmesan",              22.90m, "photo-1612874742237-6526221588e3", 25, now),
            Item(Cat("Pasta"),   "Chicken Alfredo",      "Fettuccine in rich garlic-parmesan cream with grilled chicken",       23.90m, "photo-1645112411341-6c4fd023714a", 25, now),
            Item(Cat("Pasta"),   "Aglio Olio Prawn",     "Spaghetti tossed in garlic, chilli flakes & olive oil with prawns",   21.90m, "photo-1473093295043-cdd812d0e601", 20, now),

            // Steak
            Item(Cat("Steak"),   "Grilled Ribeye Steak", "250g halal ribeye, black pepper sauce, fries & garden salad",         52.90m, "photo-1600891964092-4316c288032e", 15, now),
            Item(Cat("Steak"),   "Sirloin Steak",        "220g halal sirloin with mushroom sauce & mashed potato",              45.90m, "photo-1558030006-450675393462", 15, now),
            Item(Cat("Steak"),   "Lamb Chop",            "New Zealand lamb chops, mint gravy & roasted vegetables",             48.90m, "photo-1514516345957-556ca7d90a29", 12, now),

            // Chicken
            Item(Cat("Chicken"), "Chicken Chop",         "Grilled marinated chicken leg, brown sauce, fries & coleslaw",        19.90m, "photo-1598103442097-8b74394b95c6", 45, now),
            Item(Cat("Chicken"), "Chicken Cordon Bleu",  "Breaded chicken stuffed with smoked turkey & cheese",                 26.90m, "photo-1632778149955-e80f8ceca2e8", 20, now),

            // Pizza
            Item(Cat("Pizza"),   "Margherita Pizza",     "San Marzano tomato, mozzarella & fresh basil",                        23.90m, "photo-1574071318508-1cdbab80d002", 22, now),
            Item(Cat("Pizza"),   "BBQ Chicken Pizza",    "BBQ chicken, red onion, capsicum & mozzarella",                       27.90m, "photo-1513104890138-7c749659a591", 22, now),

            // Seafood
            Item(Cat("Seafood"), "Fish & Chips",         "Beer-free battered dory, tartar sauce, fries & mushy peas",           21.90m, "photo-1579208030886-b937da0925dc", 28, now),
            Item(Cat("Seafood"), "Garlic Butter Prawns", "Pan-seared prawns in garlic herb butter with toasted baguette",       29.90m, "photo-1565299624946-b28f40a0ae38", 18, now),

            // Dessert
            Item(Cat("Dessert"), "Sticky Date Pudding",  "Warm date sponge, butterscotch sauce & vanilla ice cream",            14.90m, "photo-1488477181946-6428a0291777", 30, now),
            Item(Cat("Dessert"), "Chocolate Lava Cake",  "Molten dark chocolate cake with a scoop of gelato",                   15.90m, "photo-1624353365286-3f8d62daad51", 30, now),

            // Beverage
            Item(Cat("Beverage"),"Iced Lemon Tea",       "Freshly brewed black tea over ice with lemon",                         7.90m, "photo-1499638673689-79a0b5115d87", 80, now),
            Item(Cat("Beverage"),"Iced Caffe Latte",     "Double espresso with chilled milk over ice",                          11.90m, "photo-1461023058943-07fcbe16d735", 60, now),
        };

        db.MenuItems.AddRange(items);
        await db.SaveChangesAsync(ct);
    }

    private static MenuItem Item(Category category, string name, string description, decimal price, string photoId, int stock, DateTimeOffset now) =>
        new()
        {
            CategoryId = category.Id,
            Name = name,
            Description = description,
            Price = price,
            ImageUrl = $"https://images.unsplash.com/{photoId}{Img}",
            StockQuantity = stock,
            Availability = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
