# Forkly.TrackerService (Alia)

The **mock** Notification & Tracking service. REST, **.NET 8 + ASP.NET Core**,
runs on **http://localhost:5500**. The customer's tracking screen polls it after
checkout to watch their order progress.

## Where it sits in the flow

```
Order (Hanif) ─ status ─► Preparing / Completed / OutForDelivery   (set by Kitchen/Zul)
Customer ─► /track/{orderId}  ─► Tracker
   GET /api/tracking/{orderId} -> reads status + paymentStatus from the Order
        service, builds the progress timeline, and — while Out for delivery —
        returns a MOCK ETA. When the timer elapses it PATCHes the Order service
        status to Delivered (Alia's responsibility).
```

Like Kitchen, the Tracker holds **no data of its own** — the Order service is the
single source of truth for status. It only *reads* status and *writes* the final
`Delivered`. The caller's JWT is forwarded so the Order service enforces ownership
(a customer can only track their own order).

## API

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/tracking/{orderId}` | Live status, progress steps, message, and (while Out for delivery) the mock ETA |

Response includes: `status`, `paymentStatus`, `message`, `steps[]`
(`confirmed → preparing → ready → out → delivered`, each `done|current|upcoming`),
`etaMinutes` / `remainingSeconds` (only while out for delivery), and `items` + `total`.

## The mock delivery timer

There is no real GPS. When an order is **OutForDelivery**, the ETA is a short,
deterministic per-order duration anchored to when it went out (the order's
`UpdatedAt`). Base is `Tracker:DeliverySeconds` (default **90s**, kept short so a
demo is watchable). When it elapses, the Tracker marks the order **Delivered**.
Alia can change the preset in config.

## Run

```bash
cd Forkly-TrackerApi
dotnet run          # http://localhost:5500/swagger
```

Needs the **Order service** on the configured `OrderService:BaseUrl`
(default `http://localhost:5208`). For the full flow, run everything with
`./start-all.ps1` from the repo root.
