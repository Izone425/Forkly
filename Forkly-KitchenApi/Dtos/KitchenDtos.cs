namespace Forkly.KitchenService.Dtos;

// A ticket on the kitchen board — the crew-facing shape of an order.
public class KitchenTicket
{
    public int OrderId { get; set; }
    public string? Reference { get; set; }
    public string Status { get; set; } = string.Empty; // Pending | Preparing | Completed | OutForDelivery (all Paid)
    public DateTimeOffset PlacedAt { get; set; }
    public decimal Total { get; set; }
    public List<KitchenLine> Items { get; set; } = new();
}

public class KitchenLine
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

// Crew action: advance a ticket. The kitchen may set Preparing/Completed/OutForDelivery.
public class StatusRequest
{
    public string Status { get; set; } = string.Empty;
}
