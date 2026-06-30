# Forkly-MenuApi — Persistent Menu Microservice with gRPC — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate the existing persistent menu service to the repo root as `Forkly-MenuApi`, add a gRPC server, and make the Order service call it over gRPC to validate items and use the authoritative DB price at checkout.

**Architecture:** `Forkly-MenuApi` (root) keeps its REST + EF Core → PostgreSQL (`menu` schema) stack and gains a gRPC server on a second Kestrel port (5102, h2c). `Forkly.OrderService` gains a gRPC client (`IMenuCatalog`) behind a `Menu:UseMock` switch; `OrderService.CreateAsync` calls it per cart line. The frontend menu store stops silently falling back to bundled sample data when the service is configured.

**Tech Stack:** .NET 8 (RollForward LatestMajor), ASP.NET Core, EF Core + Npgsql, Grpc.AspNetCore 2.80.0, generated types from `backend/contracts/menu.proto` (C# namespace `Forkly.Contracts.Menu`), xUnit, Vue 3 + Vite + Vitest.

## Global Constraints

- Target framework `net8.0` with `<RollForward>LatestMajor</RollForward>` for all .NET projects (only the .NET 10 runtime is installed).
- gRPC package versions pinned to **2.80.0** (matches the rest of the repo).
- The proto contract `backend/contracts/menu.proto` is the single source of truth — **do not edit it**. Generated types: service `Forkly.Contracts.Menu.MenuService` (base `MenuServiceBase`, client `MenuServiceClient`); message `Forkly.Contracts.Menu.MenuItem` with C# properties `Id` (string), `Name`, `Description`, `PriceCents` (int), `Category`, `Emoji`, `Available` (bool); `GetMenuRequest`, `GetItemRequest { Id }`, `GetMenuResponse { Items }`.
- Money: menu stores RM as `decimal`; the contract uses integer `price_cents`. Convert `cents = round(price * 100)` and `price = cents / 100m`.
- The menu service's internal C# namespace/assembly stays `Forkly.MenuService` (only the folder moves) to avoid churning every file. `MenuService` is an overloaded name (REST impl class, root namespace, and the generated gRPC type) — always fully-qualify the proto type as `Forkly.Contracts.Menu.MenuService`.
- Ports: menu REST `5100` (HTTP/1.1+2), menu gRPC `5102` (HTTP/2 cleartext). Avoid `5101` (legacy order service).

---

### Task 1: Relocate the menu service to the repo root

**Files:**
- Move: `backend/Forkly.MenuService/` → `Forkly-MenuApi/` (git mv, history preserved)
- Modify: `backend/Forkly.slnx`
- Modify: `start-all.ps1`

**Interfaces:**
- Produces: the menu project at `Forkly-MenuApi/Forkly.MenuService.csproj` (assembly/namespace unchanged: `Forkly.MenuService`).

- [ ] **Step 1: Move the folder with git**

```bash
git mv backend/Forkly.MenuService Forkly-MenuApi
```

- [ ] **Step 2: Remove the menu project from** `backend/Forkly.slnx`

The menu service is now a standalone root-level service (like `Forkly-Api`, which has its own solution) and is built/run on its own. The Order service depends on it only over the wire via the shared `menu.proto`, **not** via a project reference, so it does not belong in `backend/Forkly.slnx`. Delete the menu line, leaving:

```xml
<Solution>
  <Project Path="OrderService/OrderService.csproj" />
  <Project Path="Forkly.OrderService/Forkly.OrderService.csproj" />
</Solution>
```

- [ ] **Step 3: Update `start-all.ps1`** menu launch path

Change the menu Start-App line (and leave the others) to:

```powershell
Start-App 'Forkly-Menu'    (Join-Path $root 'Forkly-MenuApi')                   'dotnet run'
```

- [ ] **Step 4: Verify it still builds and serves REST**

Run: `dotnet build Forkly-MenuApi/Forkly.MenuService.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Relocate menu service to repo root as Forkly-MenuApi"
```

---

### Task 2: Add the gRPC server to Forkly-MenuApi

**Files:**
- Modify: `Forkly-MenuApi/Forkly.MenuService.csproj`
- Modify: `Forkly-MenuApi/Program.cs`
- Modify: `Forkly-MenuApi/Properties/launchSettings.json`
- Create: `Forkly-MenuApi/Services/Grpc/MenuGrpcService.cs`
- Create: `Forkly-MenuApi.Tests/Forkly-MenuApi.Tests.csproj`
- Test: `Forkly-MenuApi.Tests/MenuGrpcServiceTests.cs`

**Interfaces:**
- Consumes: `IMenuService.GetMenuAsync(bool availableOnly, CancellationToken)`, `IMenuService.GetByIdAsync(int id, CancellationToken)` returning `Forkly.MenuService.Dtos.MenuItemResponse?` (fields `Id` int, `Name`, `Description`, `UnitPrice` decimal, `Category` string, `Availability` bool).
- Produces: gRPC `Forkly.Contracts.Menu.MenuService` served on `:5102`; `public static MenuItem MapToProto(MenuItemResponse)`.

- [ ] **Step 1: Write the failing test** — create `Forkly-MenuApi.Tests/Forkly-MenuApi.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RollForward>LatestMajor</RollForward>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <!-- TestServerCallContext lives here; explicit ref de-risks the transitive path. -->
    <PackageReference Include="Grpc.Core.Api" Version="2.80.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Forkly-MenuApi\Forkly.MenuService.csproj" />
  </ItemGroup>
</Project>
```

Then `Forkly-MenuApi.Tests/MenuGrpcServiceTests.cs`:

```csharp
using Forkly.Contracts.Menu;
using Forkly.MenuService.Dtos;
using Forkly.MenuService.Services;
using Forkly.MenuService.Services.Grpc;
using Grpc.Core;
using Grpc.Core.Testing;
using Xunit;

namespace ForklyMenuApi.Tests;

public class MenuGrpcServiceTests
{
    // Minimal IMenuService stub: returns a preset item for GetByIdAsync.
    private sealed class StubMenuService : IMenuService
    {
        public MenuItemResponse? Item { get; init; }
        public Task<MenuItemResponse?> GetByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(id == Item?.Id ? Item : null);
        public Task<IReadOnlyList<MenuItemResponse>> GetMenuAsync(bool availableOnly, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MenuItemResponse>>(Item is null ? new List<MenuItemResponse>() : new List<MenuItemResponse> { Item });
        public Task<MenuItemResponse> CreateAsync(CreateMenuItemRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MenuItemResponse?> UpdateAsync(int id, UpdateMenuItemRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MenuItemResponse?> SetAvailabilityAsync(int id, bool a, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static ServerCallContext TestContext() => TestServerCallContext.Create(
        method: "test", host: null, deadline: DateTime.UtcNow.AddMinutes(1),
        requestHeaders: new Metadata(), cancellationToken: CancellationToken.None,
        peer: "test", authContext: null, contextPropagationToken: null,
        writeHeadersFunc: _ => Task.CompletedTask, writeOptionsGetter: () => new WriteOptions(),
        writeOptionsSetter: _ => { });

    [Fact]
    public void MapToProto_converts_price_to_cents_and_id_to_string()
    {
        var dto = new MenuItemResponse { Id = 7, Name = "Burger", Description = "d", UnitPrice = 18.90m, Category = "Burger", Availability = true };
        var proto = MenuGrpcService.MapToProto(dto);
        Assert.Equal("7", proto.Id);
        Assert.Equal(1890, proto.PriceCents);
        Assert.True(proto.Available);
        Assert.Equal("Burger", proto.Category);
    }

    [Fact]
    public async Task GetItem_returns_mapped_item_when_found()
    {
        var svc = new MenuGrpcService(new StubMenuService { Item = new MenuItemResponse { Id = 3, Name = "Pasta", UnitPrice = 22.00m, Availability = true } });
        var result = await svc.GetItem(new GetItemRequest { Id = "3" }, TestContext());
        Assert.Equal("3", result.Id);
        Assert.Equal(2200, result.PriceCents);
    }

    [Fact]
    public async Task GetItem_throws_NotFound_when_missing()
    {
        var svc = new MenuGrpcService(new StubMenuService { Item = null });
        var ex = await Assert.ThrowsAsync<RpcException>(() => svc.GetItem(new GetItemRequest { Id = "999" }, TestContext()));
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Forkly-MenuApi.Tests/Forkly-MenuApi.Tests.csproj`
Expected: FAIL to compile — `MenuGrpcService` does not exist yet (and the `<Protobuf>` types aren't generated). This is the correct failure.

- [ ] **Step 3: Add gRPC packages + proto to** `Forkly-MenuApi/Forkly.MenuService.csproj`

Add inside the existing package `ItemGroup`:

```xml
    <PackageReference Include="Grpc.AspNetCore" Version="2.80.0" />
```

Add a new `ItemGroup` (relative path from root `Forkly-MenuApi/` to `backend/contracts/`):

```xml
  <ItemGroup>
    <Protobuf Include="..\backend\contracts\menu.proto" GrpcServices="Server" Link="Protos\menu.proto" />
  </ItemGroup>
```

- [ ] **Step 4: Create** `Forkly-MenuApi/Services/Grpc/MenuGrpcService.cs`

```csharp
using Forkly.Contracts.Menu;
using Forkly.MenuService.Dtos;
using Forkly.MenuService.Services;
using Grpc.Core;

namespace Forkly.MenuService.Services.Grpc;

// gRPC facade over IMenuService — the parallel of MenuController. The base class
// is generated from backend/contracts/menu.proto by Grpc.Tools at build. The Order
// service is the client (it validates + prices a cart line through GetItem).
public class MenuGrpcService : Forkly.Contracts.Menu.MenuService.MenuServiceBase
{
    private readonly IMenuService _menu;

    public MenuGrpcService(IMenuService menu) => _menu = menu;

    public override async Task<GetMenuResponse> GetMenu(GetMenuRequest request, ServerCallContext context)
    {
        var items = await _menu.GetMenuAsync(availableOnly: true, context.CancellationToken);
        var response = new GetMenuResponse();
        response.Items.AddRange(items.Select(MapToProto));
        return response;
    }

    public override async Task<Forkly.Contracts.Menu.MenuItem> GetItem(GetItemRequest request, ServerCallContext context)
    {
        if (!int.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.NotFound, $"Menu item '{request.Id}' not found."));

        var item = await _menu.GetByIdAsync(id, context.CancellationToken);
        if (item is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Menu item '{request.Id}' not found."));

        return MapToProto(item);
    }

    // Entity-DTO -> proto. Price (decimal RM) -> price_cents (int). The entity has no
    // emoji (it uses ImageUrl), and consumers don't need it, so emoji is empty.
    public static Forkly.Contracts.Menu.MenuItem MapToProto(MenuItemResponse m) => new()
    {
        Id = m.Id.ToString(),
        Name = m.Name,
        Description = m.Description,
        PriceCents = (int)decimal.Round(m.UnitPrice * 100m, 0, MidpointRounding.AwayFromZero),
        Category = m.Category,
        Emoji = string.Empty,
        Available = m.Availability,
    };
}
```

- [ ] **Step 5: Wire gRPC into** `Forkly-MenuApi/Program.cs`

Add `using Microsoft.AspNetCore.Server.Kestrel.Core;` and `using Forkly.MenuService.Services.Grpc;` at the top.

Immediately after `var builder = WebApplication.CreateBuilder(args);` add:

```csharp
// 5100 — HTTP/1.1+2: REST (browser + admin CRUD).
// 5102 — HTTP/2 cleartext (h2c): native gRPC for the Order service.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5100, listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenLocalhost(5102, listen => listen.Protocols = HttpProtocols.Http2);
});
```

Next to `builder.Services.AddControllers();` add:

```csharp
builder.Services.AddGrpc();
```

After the existing `app.MapControllers();` line add:

```csharp
app.MapGrpcService<MenuGrpcService>();
```

- [ ] **Step 6: Make Kestrel ports authoritative in** `Forkly-MenuApi/Properties/launchSettings.json`

Remove the `applicationUrl` line from BOTH profiles so the explicit `ConfigureKestrel` endpoints (5100/5102) are used without conflict. The `http` profile becomes:

```json
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
```

Apply the same removal of `applicationUrl` to the `https` profile (or delete the `https` profile entirely — the service runs h2c on plain HTTP for dev).

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test Forkly-MenuApi.Tests/Forkly-MenuApi.Tests.csproj`
Expected: PASS (3 passed).

- [ ] **Step 8: Add the test project to the solution and commit**

```bash
git add -A
git commit -m "Add gRPC server (GetMenu/GetItem) to Forkly-MenuApi with tests"
```

---

### Task 3: Wire Forkly.OrderService to the menu over gRPC

**Files:**
- Modify: `backend/Forkly.OrderService/Forkly.OrderService.csproj`
- Create: `backend/Forkly.OrderService/Menu/MenuOptions.cs`
- Create: `backend/Forkly.OrderService/Menu/MenuItemInfo.cs`
- Create: `backend/Forkly.OrderService/Menu/IMenuCatalog.cs`
- Create: `backend/Forkly.OrderService/Menu/MenuUnavailableException.cs`
- Create: `backend/Forkly.OrderService/Menu/MenuGrpcCatalog.cs`
- Create: `backend/Forkly.OrderService/Menu/MockMenuCatalog.cs`
- Modify: `backend/Forkly.OrderService/Program.cs`
- Modify: `backend/Forkly.OrderService/Services/OrderService.cs`
- Modify: `backend/Forkly.OrderService/Controllers/OrdersController.cs`
- Modify: `backend/Forkly.OrderService/appsettings.json` and `appsettings.Development.json`
- Create: `backend/Forkly.OrderService.Tests/Forkly.OrderService.Tests.csproj`
- Test: `backend/Forkly.OrderService.Tests/OrderServiceCreateTests.cs`

**Interfaces:**
- Consumes: generated `Forkly.Contracts.Menu.MenuService.MenuServiceClient` (`GetItemAsync(GetItemRequest)`); `IOrderRepository.AddAsync/UpdateAsync`.
- Produces: `IMenuCatalog.GetItemAsync(int menuId, CancellationToken) -> MenuItemInfo?`; `MenuItemInfo(int Id, string Name, decimal Price, bool Available)`; `MenuUnavailableException`; new `OrderService(IOrderRepository, IMenuCatalog)` constructor.

- [ ] **Step 1: Write the failing test** — create `backend/Forkly.OrderService.Tests/Forkly.OrderService.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RollForward>LatestMajor</RollForward>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Forkly.OrderService\Forkly.OrderService.csproj" />
  </ItemGroup>
</Project>
```

Then `backend/Forkly.OrderService.Tests/OrderServiceCreateTests.cs`:

```csharp
using Forkly.OrderService.Dtos;
using Forkly.OrderService.Menu;
using Forkly.OrderService.Models;
using Forkly.OrderService.Repositories;
using Forkly.OrderService.Services;
using Xunit;

namespace Forkly.OrderService.Tests;

public class OrderServiceCreateTests
{
    private sealed class FakeMenuCatalog : IMenuCatalog
    {
        public MenuItemInfo? Result { get; init; }
        public Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
            => Task.FromResult(Result is null ? null : Result with { Id = menuId });
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Order? Saved { get; private set; }
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) { order.Id = 1; Saved = order; return Task.FromResult(order); }
        public Task UpdateAsync(Order order, CancellationToken ct = default) { Saved = order; return Task.CompletedTask; }
        public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Order?> GetByReferenceAsync(string r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Order>> GetByUserAsync(int u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Order>> GetRecentByUserAsync(int u, int c, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Order>> GetAllForReportAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<Order> Items, int Total)> GetAllAsync(string? s, int? u, int p, int ps, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task CreateAsync_uses_authoritative_menu_price_not_client_price()
    {
        var repo = new FakeOrderRepository();
        var menu = new FakeMenuCatalog { Result = new MenuItemInfo(0, "Real Burger", 20.00m, true) };
        var sut = new Forkly.OrderService.Services.OrderService(repo, menu);

        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 5, ItemName = "spoofed", Price = 0.01m, Quantity = 2 } } };
        var result = await sut.CreateAsync(userId: 42, req);

        Assert.Equal("Real Burger", result.Items[0].ItemName);
        Assert.Equal(20.00m, result.Items[0].Price);
        Assert.Equal(40.00m, result.Subtotal);   // 20.00 * 2, not 0.01 * 2
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_menu_item()
    {
        var sut = new Forkly.OrderService.Services.OrderService(new FakeOrderRepository(), new FakeMenuCatalog { Result = null });
        var req = new CreateOrderRequest { Items = { new CreateOrderItemDto { MenuId = 999, ItemName = "x", Price = 1m, Quantity = 1 } } };
        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(1, req));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Forkly.OrderService.Tests/Forkly.OrderService.Tests.csproj`
Expected: FAIL to compile — `IMenuCatalog`/`MenuItemInfo` don't exist and `OrderService` has no 2-arg constructor. Correct failure.

- [ ] **Step 3: Add gRPC client packages + proto to** `backend/Forkly.OrderService/Forkly.OrderService.csproj`

Add to the package `ItemGroup`:

```xml
    <PackageReference Include="Grpc.AspNetCore" Version="2.80.0" />
    <PackageReference Include="Grpc.Net.ClientFactory" Version="2.80.0" />
```

Add a new `ItemGroup` (path from `backend/Forkly.OrderService/` to `backend/contracts/`):

```xml
  <ItemGroup>
    <Protobuf Include="..\contracts\menu.proto" GrpcServices="Client" Link="Protos\menu.proto" />
  </ItemGroup>
```

- [ ] **Step 4: Create the catalog files** under `backend/Forkly.OrderService/Menu/`

`MenuOptions.cs`:
```csharp
namespace Forkly.OrderService.Menu;

// Bound from the "Menu" config section. UseMock (or an empty GrpcAddress) selects
// the offline MockMenuCatalog; otherwise the real gRPC client is used.
public class MenuOptions
{
    public bool UseMock { get; set; }
    public string GrpcAddress { get; set; } = string.Empty;
}
```

`MenuItemInfo.cs`:
```csharp
namespace Forkly.OrderService.Menu;

// Authoritative menu facts the Order service needs to price/validate a cart line.
// Price is in RM (decimal), converted from the contract's price_cents.
public record MenuItemInfo(int Id, string Name, decimal Price, bool Available);
```

`IMenuCatalog.cs`:
```csharp
namespace Forkly.OrderService.Menu;

// The Order service's view of the Menu service. Returns null when the item does
// not exist; throws MenuUnavailableException when the menu cannot be reached.
public interface IMenuCatalog
{
    Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default);
}
```

`MenuUnavailableException.cs`:
```csharp
namespace Forkly.OrderService.Menu;

// Thrown when the Menu service cannot be reached (as opposed to the item simply
// not existing). The controller maps this to HTTP 503.
public class MenuUnavailableException : Exception
{
    public MenuUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
```

`MenuGrpcCatalog.cs`:
```csharp
using Forkly.Contracts.Menu;
using Grpc.Core;

namespace Forkly.OrderService.Menu;

// Real catalog backed by the Menu service's gRPC endpoint (menu.proto GetItem).
public sealed class MenuGrpcCatalog : IMenuCatalog
{
    private readonly Forkly.Contracts.Menu.MenuService.MenuServiceClient _client;

    public MenuGrpcCatalog(Forkly.Contracts.Menu.MenuService.MenuServiceClient client) => _client = client;

    public async Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
    {
        try
        {
            var item = await _client.GetItemAsync(new GetItemRequest { Id = menuId.ToString() }, cancellationToken: ct);
            return new MenuItemInfo(menuId, item.Name, item.PriceCents / 100m, item.Available);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            throw new MenuUnavailableException("The menu service is unavailable. Please try again.", ex);
        }
    }
}
```

`MockMenuCatalog.cs`:
```csharp
namespace Forkly.OrderService.Menu;

// Offline stand-in used when Menu:UseMock=true (or no GrpcAddress). Echoes the
// requested item so local demos run without the Menu service; price is NOT
// authoritative (0). Do not use where real pricing matters.
public sealed class MockMenuCatalog : IMenuCatalog
{
    public Task<MenuItemInfo?> GetItemAsync(int menuId, CancellationToken ct = default)
        => Task.FromResult<MenuItemInfo?>(new MenuItemInfo(menuId, $"Item {menuId}", 0m, true));
}
```

- [ ] **Step 5: Register the catalog in** `backend/Forkly.OrderService/Program.cs`

Add `using Forkly.OrderService.Menu;` and `using Forkly.Contracts.Menu;` at the top. After the existing `builder.Services.AddScoped<IReportService, ReportService>();` line add:

```csharp
// --- Menu client: real gRPC when configured, else an offline mock (Menu:UseMock). ---
builder.Services.Configure<MenuOptions>(builder.Configuration.GetSection("Menu"));
var menuOptions = builder.Configuration.GetSection("Menu").Get<MenuOptions>() ?? new MenuOptions();
if (menuOptions.UseMock || string.IsNullOrWhiteSpace(menuOptions.GrpcAddress))
{
    builder.Services.AddSingleton<IMenuCatalog, MockMenuCatalog>();
}
else
{
    builder.Services.AddGrpcClient<Forkly.Contracts.Menu.MenuService.MenuServiceClient>(o =>
        o.Address = new Uri(menuOptions.GrpcAddress));
    builder.Services.AddScoped<IMenuCatalog, MenuGrpcCatalog>();
}
```

- [ ] **Step 6: Validate + authoritative price in** `backend/Forkly.OrderService/Services/OrderService.cs`

Add `using Forkly.OrderService.Menu;`. Add a field and extend the constructor:

```csharp
    private readonly IOrderRepository _repo;
    private readonly IMenuCatalog _menu;

    public OrderService(IOrderRepository repo, IMenuCatalog menu)
    {
        _repo = repo;
        _menu = menu;
    }
```

In `CreateAsync`, replace the `var items = request.Items.Select(...).ToList();` block with:

```csharp
        var items = new List<OrderItem>();
        foreach (var i in request.Items)
        {
            var info = await _menu.GetItemAsync(i.MenuId, ct);
            if (info is null)
                throw new ArgumentException($"Menu item {i.MenuId} does not exist.");
            if (!info.Available)
                throw new ArgumentException($"'{info.Name}' is currently unavailable.");

            items.Add(new OrderItem
            {
                MenuId = i.MenuId,
                ItemName = info.Name,   // authoritative name from the Menu service
                Price = info.Price,     // authoritative price — the client-sent price is ignored
                Quantity = i.Quantity,
            });
        }
```

(The subtotal/SST/total computation below is unchanged — it now sums authoritative prices.)

- [ ] **Step 7: Map the unavailable case to 503 in** `backend/Forkly.OrderService/Controllers/OrdersController.cs`

Add `using Forkly.OrderService.Menu;`. In the `Create` action, add a catch after the existing `catch (ArgumentException ex)` block:

```csharp
        catch (MenuUnavailableException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
```

- [ ] **Step 8: Add the `Menu` config section**

`backend/Forkly.OrderService/appsettings.json` — add after the `Jwt` block:
```json
  ,
  "Menu": {
    "UseMock": false,
    "GrpcAddress": ""
  }
```

`backend/Forkly.OrderService/appsettings.Development.json` — add after the `Jwt` block:
```json
  ,
  "Menu": {
    "UseMock": false,
    "GrpcAddress": "http://localhost:5102"
  }
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test backend/Forkly.OrderService.Tests/Forkly.OrderService.Tests.csproj`
Expected: PASS (2 passed).

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "Wire OrderService to menu via gRPC: validate items + authoritative pricing"
```

---

### Task 4: Frontend — stop silent mock fallback when the service is configured

**Files:**
- Modify: `src/stores/menu.js`
- Modify: `package.json` (add Vitest)
- Create: `vitest.config.js`
- Test: `src/stores/menu.test.js`
- Verify: `.env.development` already sets `VITE_MENU_API_BASE=http://localhost:5100` (from the merged FE branch) — confirm, no change expected.

**Interfaces:**
- Consumes: `fetchMenu()`, `isMenuApiConfigured()` from `src/services/menuApi.js`.
- Produces: `useMenu()` store whose `state.source` is `'api' | 'error' | 'fallback'`.

- [ ] **Step 1: Add Vitest to** `package.json`

Add a test script and devDependencies:
```json
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview",
    "test": "vitest run"
  },
```
Add to `devDependencies`: `"vitest": "^2.1.8"`. Then run `npm install`.

- [ ] **Step 2: Create** `vitest.config.js`

```javascript
import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.js'],
  },
})
```

- [ ] **Step 3: Write the failing test** `src/stores/menu.test.js`

```javascript
import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock the menu API the store depends on.
vi.mock('../services/menuApi.js', () => ({
  isMenuApiConfigured: vi.fn(),
  fetchMenu: vi.fn(),
}))
// Keep the fallback import resolvable.
vi.mock('../data/menu.js', () => ({ MENU: [{ id: 'sample', name: 'Sample' }] }))

import { fetchMenu, isMenuApiConfigured } from '../services/menuApi.js'
import { useMenu } from './menu.js'

describe('menu store', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('shows an error/empty state (not the sample menu) when configured but the API fails', async () => {
    isMenuApiConfigured.mockReturnValue(true)
    fetchMenu.mockRejectedValue(new Error('down'))

    const { state, load } = useMenu()
    await load(true)

    expect(state.source).toBe('error')
    expect(state.items).toEqual([])
    expect(state.error).toBeTruthy()
  })
})
```

- [ ] **Step 4: Run test to verify it fails**

Run: `npm run test`
Expected: FAIL — current store sets `state.source = 'fallback'` and `state.items = FALLBACK` on error.

- [ ] **Step 5: Update** `src/stores/menu.js`

Replace the `catch (e) { ... }` block inside `load()` with:

```javascript
  } catch (e) {
    // The service is configured but unreachable. Surface an error and show nothing
    // rather than silently swapping in the bundled sample menu (which can't be saved).
    state.error = e?.message || 'Could not load the menu.'
    state.items = []
    state.source = 'error'
    state.loaded = true
  } finally {
```

(Leave the `if (!isMenuApiConfigured())` branch that returns the bundled `FALLBACK` unchanged — that only runs when no menu service is configured at all.)

- [ ] **Step 6: Run test to verify it passes**

Run: `npm run test`
Expected: PASS (1 passed).

- [ ] **Step 7: Confirm `.env.development`** contains `VITE_MENU_API_BASE=http://localhost:5100` (already added on the merged FE branch). If missing, add it.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Frontend: show error state instead of silent mock fallback when menu is configured"
```

---

### Task 5: Tooling + end-to-end verification

**Files:**
- Modify: `start-all.ps1` (add the Order service; refresh the port banner)

- [ ] **Step 1: Add the Order service to** `start-all.ps1`

In the Start-App block add an order line and update the free-port list to include 5102:
```powershell
Start-App 'Forkly-API'     (Join-Path $root 'Forkly-Api')                       'dotnet run'
Start-App 'Forkly-Menu'    (Join-Path $root 'Forkly-MenuApi')                   'dotnet run'
Start-App 'Forkly-Order'   (Join-Path $root 'backend/Forkly.OrderService')      'dotnet run'
Start-App 'Forkly-Landing' $root                                                'npm run dev'
```
Add `5102` to the `Free-Port` port list near the top.

- [ ] **Step 2: Build the backend solution and the (now standalone) menu service**

Run: `dotnet build backend/Forkly.slnx` (order + legacy services) and `dotnet build Forkly-MenuApi/Forkly.MenuService.csproj` (menu, no longer in the solution).
Expected: both Build succeeded.

- [ ] **Step 3: Run all .NET tests**

Run: `dotnet test Forkly-MenuApi.Tests/Forkly-MenuApi.Tests.csproj` and `dotnet test backend/Forkly.OrderService.Tests/Forkly.OrderService.Tests.csproj`
Expected: all green.

- [ ] **Step 4: Manual end-to-end (documented in the spec's Verification section)**
  - Start `Forkly-MenuApi` (REST 5100 + gRPC 5102) and `Forkly.OrderService`.
  - In Swagger (menu) or `/admin/menu`, create an item → reload → it persists (survives a service restart, proving DB persistence).
  - Via the Order service Swagger, `POST /api/orders` (with a valid JWT) using a real `menuId` → succeeds; the returned line `price` equals the menu's DB price even if a different price was sent. A bogus `menuId` → 400.
  - Stop the menu service, retry the order create → 503 (not a silently-trusted order).
  - Frontend: landing/order pages show DB items; with the menu service stopped, the UI shows an error/empty state, never the 6 sample items.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "start-all: launch Order service; finalize menu gRPC ports"
```

---

## Self-Review

**Spec coverage:**
- Relocate to root `Forkly-MenuApi` → Task 1. ✓
- gRPC server (GetMenu/GetItem, mapping incl. price_cents) → Task 2. ✓
- OrderService gRPC client + validate existence + authoritative price + 503 on unavailable → Task 3. ✓
- Frontend stop silent mock fallback → Task 4. ✓
- Persistence (unchanged EF→Postgres `menu` schema) → preserved by Task 1 (no DB changes). ✓
- Tooling / start-all / e2e verification → Task 5. ✓

**Placeholder scan:** No TBD/TODO; every code step shows full code; commands have expected output. ✓

**Type consistency:** `IMenuCatalog.GetItemAsync(int)→MenuItemInfo?`, `MenuItemInfo(int,string,decimal,bool)`, `MenuGrpcService.MapToProto(MenuItemResponse)→Forkly.Contracts.Menu.MenuItem`, `OrderService(IOrderRepository, IMenuCatalog)` — used consistently across tasks 2/3 and their tests. Proto property names (`PriceCents`, `Available`, `Id`) match the generated types confirmed from the legacy `MenuGrpcClient`. ✓

**Open risk (verify at execution):** h2c gRPC client→server on plain HTTP works in this repo without the `Http2UnencryptedSupport` AppContext switch (the legacy OrderService calls payment gRPC the same way). If the channel fails to negotiate HTTP/2, add `AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);` at the top of the Order service `Program.cs`.
