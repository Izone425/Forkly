using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Forkly.OrderService.Data;

// Lets `dotnet ef migrations add` / `database update` construct the context
// without starting the full web host (which would require JWT config etc.).
// Reads the same appsettings files the app uses.
public class OrderDbContextDesignFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var conn = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn))
            conn = "Host=localhost;Port=5432;Database=foodorder;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(conn, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", OrderDbContext.Schema))
            .Options;

        return new OrderDbContext(options);
    }
}
