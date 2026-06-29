# Forkly.OrderService

The Order Service for Forkly — creates orders, stores order items, and serves
order history / recent orders / reorder data. REST API, **.NET 8 + ASP.NET Core**,
**EF Core (Npgsql / PostgreSQL)**, clean-architecture layering.

> Scope: this service owns **orders only**. It does not own users, menu, or
> payment, and it does not touch other services' tables.

## Storage model — important

It uses the shared SIT **`foodorder`** database but its **own isolated PostgreSQL
schema `order`** (`order.Orders`, `order.OrderItems`) with its own
`order.__EFMigrationsHistory`. It therefore **never collides** with the User
service (`Forkly-Api`), which owns `public.Orders` / `public.OrderItems`.

## Architecture

```
Controllers/   OrdersController        REST endpoints + JWT user-id extraction
Services/      IOrderService/OrderService   business logic (SST 6%, reorder)
Repositories/  IOrderRepository/OrderRepository   EF Core data access
Data/          OrderDbContext          maps to the "order" schema
Dtos/          request/response shapes (EF entities are never exposed)
Models/        Order, OrderItem, OrderStatus
```

## Business rules

- `Subtotal` = Σ (item `Price` × `Quantity`), computed **server-side**.
- `Sst` = 6% of `Subtotal`.
- `Total` = `Subtotal` + `Sst`.
- Client-supplied totals are ignored; only line items are trusted.

## Auth

Every endpoint requires a Bearer JWT **issued by the User service (`Forkly-Api`)**.
This service validates it with the same Issuer / Audience / signing key
(`Jwt:*` in config) and reads the user id from the `NameIdentifier` (`sub`) claim.
There is no login here.

## API

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/orders` | Create an order from cart items |
| GET  | `/api/orders/{orderId}` | Full order + items (caller's own) |
| GET  | `/api/orders/user/{userId}` | All of a user's orders, newest first |
| GET  | `/api/orders/user/{userId}/recent` | The user's 3 most recent orders |
| POST | `/api/orders/reorder/{orderId}` | Returns a past order's items to merge into the cart (writes nothing) |

### Create order — request body
```json
{
  "items": [
    { "menuId": 1, "itemName": "Burger", "price": 10.00, "quantity": 2 },
    { "menuId": 4, "itemName": "Coffee", "price": 6.00, "quantity": 1 }
  ]
}
```

## Run

```bash
# from backend/Forkly.OrderService
dotnet restore
dotnet run            # Swagger at http://localhost:5208/swagger
```

## Migrations

The `InitialCreate` migration creates the `order` schema and its two tables.
Apply it to the SIT DB (creates only `order.*` — never touches `public.*`):

```bash
dotnet tool install --global dotnet-ef      # once, if not installed
dotnet ef database update                   # applies to the configured DB
```

To regenerate from scratch:

```bash
dotnet ef migrations add InitialCreate -o Migrations
```
