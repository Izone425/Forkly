namespace Forkly.TrackerService.Dtos;

// What the customer's tracking screen renders.
public class TrackingResponse
{
    public int OrderId { get; set; }
    public string? Reference { get; set; }
    public string Status { get; set; } = string.Empty;         // fulfilment status
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Message { get; set; } = string.Empty;        // customer-facing notification text

    // Set only while Out for delivery (mock timer).
    public int? EtaMinutes { get; set; }
    public int? RemainingSeconds { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public List<TrackingStep> Steps { get; set; } = new();
    public List<TrackingLine> Items { get; set; } = new();
}

// A stage on the progress timeline.
public class TrackingStep
{
    public string Key { get; set; } = string.Empty;   // confirmed | preparing | ready | out | delivered
    public string Label { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;  // done | current | upcoming
}

public class TrackingLine
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
