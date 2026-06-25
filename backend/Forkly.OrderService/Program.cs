using Forkly.OrderService.Application.External;
using Forkly.OrderService.Application.Interfaces;
using Forkly.OrderService.Application.Services;
using Forkly.OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- MVC + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- EF Core / PostgreSQL ---
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Clean architecture wiring: Controller -> Service -> Repository ---
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// --- Mock external clients (replaced by gRPC later) ---
// TODO: gRPC calls to User Service / Menu Service / Payment / Kitchen / Tracking.
builder.Services.AddSingleton<IMenuServiceClient, MockMenuServiceClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Apply migrations + seed mock data for local development.
    using var scope = app.Services.CreateScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
