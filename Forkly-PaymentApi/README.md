# Forkly.PaymentService (Aiman)

The **mock** Payment service for Forkly. REST, **.NET 8 + ASP.NET Core**, runs on
**http://localhost:5300**. No real charge happens — it simulates the gateway — but
the flow is fully functional and wired into the other services.

## Where it sits in the flow

```
Customer places order ─► Order service (Hanif): order created, status = Pending
Frontend ─► /payment/{orderId}  ─► Payment service
   POST /api/payments/checkout   -> reads the amount from the Order service
   POST /api/payments/{id}/pay   -> "charges" (mock), then:
        └─► PATCH Order /status = "Paid"   (Order service = single source of truth)
Kitchen (Zul) ─► polls the Order service for Paid orders   (no direct call to Payment)
```

Payment's only server-to-server integration is with the **Order service** — it
reads the amount and marks the order Paid. Kitchen and Tracker read status from the
Order service, so Payment never calls them directly.

## API

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/payments/checkout` | Start a payment for an order: `{ "orderId": 12 }` → pending payment |
| POST | `/api/payments/{paymentId}/pay` | Settle it: `{ "method":"card", "cardLast4":"4242", "simulateFailure":false }` |
| GET  | `/api/payments/{paymentId}` | Payment status |
| GET  | `/api/payments/order/{orderId}` | Latest payment for an order (others can check) |

All endpoints require a Bearer JWT (the same one the User service issues). The
amount is **read from the Order service**, never trusted from the client. The
caller's token is **forwarded** to the Order service so it applies ownership checks.

## Storage

In-memory (a `ConcurrentDictionary`) — payments reset on restart. Swap for EF Core
+ a `payment` schema later without touching the controller/service.

## Run

```bash
cd Forkly-PaymentApi
dotnet run          # http://localhost:5300/swagger
```

Needs the **Order service** on 5300's configured `OrderService:BaseUrl`
(default `http://localhost:5208`). For the full flow, run everything with
`./start-all.ps1` from the repo root.

## Config

- `Jwt:Key` / `Issuer` / `Audience` — must match the User service (in
  `appsettings.Development.json`).
- `OrderService:BaseUrl` — where to reach the Order service.
