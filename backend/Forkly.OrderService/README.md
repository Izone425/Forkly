# Forkly — Order Service (Hanif module)

The core Order Service for Forkly: order creation, history, recent orders,
reorder, and basic status tracking. **.NET 8 Web API + EF Core + PostgreSQL**,
clean architecture (Controller → Service → Repository), REST only.

> This is a standalone, production-style service. It is separate from the
> earlier `backend/OrderService` gRPC scaffold and runs on its own port (5200).
> gRPC to other services is **not** implemented yet — external IDs are mocked.

## Architecture

```
Controllers/            HTTP endpoints (thin)            -> IOrderService
Application/
  Interfaces/           IOrderService, IOrderRepository
  Services/             OrderService (business logic)
  DTOs/                 request/response shapes (EF models never exposed)
  Mapping/              entity -> DTO
  External/             IMenuServiceClient + MockMenuServiceClient (gRPC later)
  Exceptions/           ValidationException (-> HTTP 400)
Domain/
  Entities/             Order, OrderItem, OrderSnapshot
  Enums/                OrderStatus
Infrastructure/
  Data/                 OrderDbContext, OrderRepository, DbInitializer (seed)
Program.cs              DI wiring, Swagger, EF Core/PostgreSQL
```

Flow: `Controller → IOrderService → IOrderRepository → OrderDbContext (PostgreSQL)`.
All methods are `async/await`; everything crosses the API boundary as DTOs.

## Prerequisites

- .NET 8 SDK
- PostgreSQL running locally (or update the connection string)

## Configuration

`appsettings.json` → `ConnectionStrings:DefaultConnection`:

```
Host=localhost;Port=5432;Database=forkly_orders;Username=postgres;Password=postgres
```

## Database migrations

```bash
cd backend/Forkly.OrderService

# one-time: EF Core CLI
dotnet tool install --global dotnet-ef

# create the schema migration from the EF models
dotnet ef migrations add InitialCreate

# apply it to PostgreSQL
dotnet ef database update
```

In **Development**, the app also auto-applies migrations and seeds a few mock
orders on startup (see `DbInitializer`). If the DB isn't reachable it logs a
warning and still serves Swagger.

## Run

```bash
dotnet run
```

- API:     http://localhost:5200
- Swagger: http://localhost:5200/swagger

## Endpoints

| Method | Path                              | Purpose                                   |
|--------|-----------------------------------|-------------------------------------------|
| POST   | `/api/orders`                     | Create an order                           |
| GET    | `/api/orders/{orderId}`           | Get one order                             |
| GET    | `/api/orders/user/{userId}`       | Order history for a user                  |
| GET    | `/api/orders/user/{userId}/recent`| Last 3 orders (quick reorder)             |
| POST   | `/api/orders/reorder/{orderId}`   | Items to **merge into cart** (no new order) |
| PUT    | `/api/orders/{orderId}/status`    | Update order status                       |

### Create order

```http
POST /api/orders
{
  "userId": 1,
  "items": [
    { "menuId": 1, "quantity": 2 },
    { "menuId": 3, "quantity": 1 }
  ]
}
```

Item names and prices are snapshotted from the Menu Service (mock) at order
time; `TotalAmount` is computed server-side. New orders start as `CREATED`.

### Reorder (important)

`POST /api/orders/reorder/{orderId}` does **not** create an order. It returns the
previous order's items so the frontend can MERGE them into the current cart:

```json
{
  "sourceOrderId": "....",
  "sourceCreatedAt": "....",
  "items": [
    { "menuId": 1, "itemName": "Classic Burger", "quantity": 2, "price": 10.00 }
  ]
}
```

### Update status

```http
PUT /api/orders/{orderId}/status
{ "status": "PAID" }
```

Valid: `CREATED, PAID, PREPARING, READY, COMPLETED, CANCELLED`
(flow: CREATED → PAID → PREPARING → READY → COMPLETED). Invalid → 400.

## Future integration (placeholders only)

Marked with `// TODO:` in code — not implemented yet:

- gRPC → **User Service** (validate userId) — `OrderService.CreateOrderAsync`
- gRPC → **Menu Service** (item name/price) — replaces `MockMenuServiceClient`
- event push → **Alia tracking service** (order created / status changed)
- later: Payment Service, Kitchen Service

External IDs (`userId`, `menuId`) are plain mock IDs until those services exist.
