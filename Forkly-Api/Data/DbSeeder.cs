using Forkly.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Forkly.Api.Data;

// Ensures the two roles exist and a default admin account is present so the
// "admin" login path is usable out of the box. Also seeds a few sample orders
// for the account "Order history" view (dev only). Runs once at startup.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        // Optional bootstrap admin — only created if both settings are provided.
        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
            && await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Forkly Admin",
                EmailConfirmed = true,
            };
            var created = await userManager.CreateAsync(admin, adminPassword);
            if (created.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        // DEV-only sample order history. Off for shared environments (e.g. SIT)
        // via Seed:SampleOrders=false so it doesn't attach fake orders to real users.
        if (config.GetValue("Seed:SampleOrders", true))
            await SeedSampleOrdersAsync(services, userManager);
    }

    // DEV ONLY: gives every user who has no orders a few sample past orders, so the
    // account "Order history" view is populated before the real ordering module
    // exists. Idempotent — skips any user that already has orders. The ordering
    // module owns real order creation; remove/disable this once it ships.
    private static async Task SeedSampleOrdersAsync(
        IServiceProvider services, UserManager<ApplicationUser> userManager)
    {
        var db = services.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var reference = 1001;

        foreach (var user in await userManager.Users.ToListAsync())
        {
            if (await db.Orders.AnyAsync(o => o.UserId == user.Id))
                continue;

            db.Orders.AddRange(
                BuildOrder(user.Id, $"FRK-{reference++}", OrderStatus.Delivered, now.AddDays(-14),
                    new[] { ("Burger", 10m, 2), ("Fries", 5m, 1), ("Coffee", 6m, 1) }),
                BuildOrder(user.Id, $"FRK-{reference++}", OrderStatus.Delivered, now.AddDays(-5),
                    new[] { ("Burger", 10m, 1), ("Coffee", 6m, 2) }),
                BuildOrder(user.Id, $"FRK-{reference++}", OrderStatus.Preparing, now.AddDays(-1),
                    new[] { ("Fries", 5m, 2), ("Coffee", 6m, 1) }));
        }

        await db.SaveChangesAsync();
    }

    private static Order BuildOrder(
        int userId, string reference, string status, DateTimeOffset placedAt,
        (string Name, decimal UnitPrice, int Quantity)[] items)
    {
        var order = new Order
        {
            UserId = userId,
            Reference = reference,
            Status = status,
            PlacedAt = placedAt,
            Currency = "MYR",
            Items = items
                .Select(i => new OrderItem { Name = i.Name, UnitPrice = i.UnitPrice, Quantity = i.Quantity })
                .ToList(),
        };
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        return order;
    }
}
