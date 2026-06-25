# Forkly — Backend (Order Service)

.NET 10 backend for the Forkly ordering system. This service owns **orders**
and coordinates the menu and payment services.

```
Browser (Vue)  --REST/JSON-->  Order Service  --gRPC-->  Menu Service   (amirul-menu, not ready -> MOCK)
                                     |
                                     +----------gRPC-->  Payment Service (other PIC, not ready -> MOCK)

Other backends --gRPC--> Order Service   (order.proto server)
```

- **Frontend ↔ Order**: REST (browsers can't speak raw gRPC).
- **Order ↔ Menu / Payment**: gRPC (backend-to-backend).
- **Order is also a gRPC server** (`order.proto`) so other backends can read/create orders.

Until amirul-menu and the payment service exist, the Order service uses **mock
clients** behind the real interfaces. Swap to live gRPC with config only — no
code change.

## Run

```bash
cd backend/OrderService
dotnet run
```

- REST API:  http://localhost:5100
- gRPC:      http://localhost:5101  (HTTP/2 h2c)

Quick check:

```bash
curl http://localhost:5100/health
curl http://localhost:5100/v1/menu
curl -X POST http://localhost:5100/v1/orders \
  -H "Content-Type: application/json" \
  -d '{"items":[{"id":"burger","quantity":2},{"id":"fries","quantity":1}]}'
```

## REST API

| Method | Path              | Purpose                                            |
|--------|-------------------|----------------------------------------------------|
| GET    | `/health`         | Liveness                                           |
| GET    | `/v1/menu`        | Menu (proxied from the menu service / mock)        |
| POST   | `/v1/orders`      | Create order → prices server-side, creates payment |
| GET    | `/v1/orders/{id}` | Fetch an order                                     |

`POST /v1/orders` body: `{ "items": [ { "id": "burger", "quantity": 2 } ] }`.
Prices are **never** trusted from the client — they come from the menu service.
Response includes `paymentRedirectUrl` for the frontend → payment handoff.

## gRPC contracts (`backend/contracts/`)

| File           | Owner            | Order's role |
|----------------|------------------|--------------|
| `menu.proto`   | amirul-menu      | **client**   |
| `payment.proto`| payment PIC      | **client**   |
| `order.proto`  | this service     | **server**   |

Money crosses services as **integer cents** (`price_cents`) to avoid float
rounding. The REST layer converts to decimal RM for the browser.

## Connecting the real services (later)

In `appsettings.json` (or env vars):

```jsonc
"Menu":    { "UseMock": false, "GrpcAddress": "http://localhost:6001" },
"Payment": { "UseMock": false, "GrpcAddress": "http://localhost:6002",
             "PaymentPageUrl": "http://localhost:5176/payment" }
```

`MockMenuClient`/`MockPaymentClient` are then replaced by `MenuGrpcClient`/
`PaymentGrpcClient` automatically (see `Program.cs`).

## Frontend wiring

The Vue app calls this API when `VITE_ORDER_API_BASE` is set (e.g. in
`.env.local`):

```
VITE_ORDER_API_BASE=http://localhost:5100
```

Without it, the order page stays in local demo mode.

## Layout

```
backend/
  Forkly.slnx
  contracts/        menu.proto · payment.proto · order.proto  (shared)
  OrderService/
    Program.cs                  DI, CORS, REST endpoints, gRPC mapping, Kestrel
    Api/Dtos.cs                 REST shapes + cents→RM
    Menu/                       IMenuClient · MockMenuClient · MenuGrpcClient
    Payment/                    IPaymentClient · MockPaymentClient · PaymentGrpcClient
    Orders/                     OrderManager · OrderStore · OrderGrpcService · models
    Configuration/Options.cs    Menu/Payment options
```
