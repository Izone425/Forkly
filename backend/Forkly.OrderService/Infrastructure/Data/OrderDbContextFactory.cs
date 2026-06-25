using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Forkly.OrderService.Infrastructure.Data;

// Lets `dotnet ef migrations add` / `database update` build the context without
// starting the web host. Reads the same connection string from appsettings.json.
public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new OrderDbContext(options);
    }
}
