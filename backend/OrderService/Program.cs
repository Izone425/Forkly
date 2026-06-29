using Forkly.Contracts.Menu;
using Forkly.Contracts.Payment;
using OrderService.Api;
using OrderService.Configuration;
using OrderService.Menu;
using OrderService.Orders;
using OrderService.Payment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MenuOptions>(builder.Configuration.GetSection("Menu"));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection("Payment"));

var menuOptions = builder.Configuration.GetSection("Menu").Get<MenuOptions>() ?? new MenuOptions();
var paymentOptions = builder.Configuration.GetSection("Payment").Get<PaymentOptions>() ?? new PaymentOptions();

// --- Menu client: mock today, real gRPC when amirul-menu is ready ---
if (menuOptions.UseMock || string.IsNullOrWhiteSpace(menuOptions.GrpcAddress))
{
    builder.Services.AddSingleton<IMenuClient, MockMenuClient>();
}
else
{
    builder.Services.AddGrpcClient<MenuService.MenuServiceClient>(o => o.Address = new Uri(menuOptions.GrpcAddress));
    builder.Services.AddScoped<IMenuClient, MenuGrpcClient>();
}

// --- Payment client: mock today, real gRPC when the payment team is ready ---
if (paymentOptions.UseMock || string.IsNullOrWhiteSpace(paymentOptions.GrpcAddress))
{
    builder.Services.AddSingleton<IPaymentClient, MockPaymentClient>();
}
else
{
    builder.Services.AddGrpcClient<PaymentService.PaymentServiceClient>(o => o.Address = new Uri(paymentOptions.GrpcAddress));
    builder.Services.AddScoped<IPaymentClient, PaymentGrpcClient>();
}

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddScoped<OrderManager>();
builder.Services.AddGrpc(); // gRPC server (order.proto)

// CORS so the Vue dev server can call the REST API.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5176" };
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors("frontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "order" }));

// --- REST API (browser) ---

// Menu — proxies the menu service (mock for now).
app.MapGet("/v1/menu", async (IMenuClient menu, CancellationToken ct) =>
{
    var items = await menu.GetMenuAsync(ct);
    return Results.Ok(items.Select(i =>
        new MenuItemResponse(i.Id, i.Name, i.Description, Money.Rm(i.PriceCents), i.Category, i.Emoji, i.Available)));
});

// Create order — prices server-side, creates payment, returns redirect URL.
app.MapPost("/v1/orders", async (CreateOrderRequestDto body, OrderManager manager, CancellationToken ct) =>
{
    try
    {
        var record = await manager.CreateOrderAsync(body.Items.Select(i => (i.Id, i.Quantity)), ct);
        return Results.Ok(ToResponse(record));
    }
    catch (OrderValidationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Fetch an order (e.g. payment page confirming status).
app.MapGet("/v1/orders/{id}", (string id, OrderManager manager) =>
{
    var record = manager.Get(id);
    return record is null ? Results.NotFound() : Results.Ok(ToResponse(record));
});

// --- gRPC server (other backends) ---
app.MapGrpcService<OrderGrpcService>();

app.Run();

static OrderResponse ToResponse(OrderRecord r) => new(
    r.OrderId,
    r.Items.Select(l => new OrderItemResponse(
        l.Id, l.Name, Money.Rm(l.UnitPriceCents), l.Quantity, Money.Rm(l.LineTotalCents))).ToList(),
    Money.Rm(r.SubtotalCents),
    Money.Rm(r.TaxCents),
    Money.Rm(r.TotalCents),
    r.Status,
    r.PaymentId,
    r.PaymentRedirectUrl);
