using Forkly.OrderService.Domain.Entities;
using Forkly.OrderService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Forkly.OrderService.Infrastructure.Data;

// Dev-only helper: applies migrations and seeds a little mock data so the API
// is usable immediately. Safe to call on every startup (seeds only when empty).
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db = services.GetRequiredService<OrderDbContext>();

        try
        {
            await db.Database.MigrateAsync();
            await SeedAsync(db);
        }
        catch (Exception ex)
        {
            // Don't crash startup if the DB isn't ready — the API still serves
            // Swagger so the contract can be inspected.
            logger.LogWarning(ex,
                "Database init skipped. Ensure PostgreSQL is running and a migration exists " +
                "(dotnet ef migrations add InitialCreate).");
        }
    }

    private static async Task SeedAsync(OrderDbContext db)
    {
        if (await db.Orders.AnyAsync()) return;

        var now = DateTime.UtcNow;

        var o1 = BuildOrder(1, OrderStatus.Completed, now.AddDays(-3),
            (1, "Classic Burger", 2, 10.00m), (3, "Fries", 1, 5.00m));

        var o2 = BuildOrder(1, OrderStatus.Completed, now.AddDays(-1),
            (3, "Fries", 2, 5.00m), (5, "Coffee", 1, 6.00m));

        var o3 = BuildOrder(1, OrderStatus.Preparing, now.AddHours(-2),
            (1, "Classic Burger", 1, 10.00m), (2, "Chicken Wings", 1, 12.00m), (6, "Soft Drink", 2, 4.00m));

        db.Orders.AddRange(o1, o2, o3);
        await db.SaveChangesAsync();
    }

    private static Order BuildOrder(
        int userId, OrderStatus status, DateTime createdAt,
        params (int MenuId, string Name, int Qty, decimal Price)[] lines)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            CreatedAt = createdAt,
        };

        foreach (var l in lines)
        {
            order.Items.Add(new OrderItem
            {
                MenuId = l.MenuId,
                ItemName = l.Name,
                Quantity = l.Qty,
                Price = l.Price,
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.Price * i.Quantity);
        order.Snapshots.Add(new OrderSnapshot { OrderId = order.OrderId, CreatedAt = createdAt });
        return order;
    }
}
