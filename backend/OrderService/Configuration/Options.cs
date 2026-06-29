namespace OrderService.Configuration;

// Bound from the "Menu" config section.
public sealed class MenuOptions
{
    // When true (default), use the in-memory MockMenuClient.
    // Set false + GrpcAddress to call amirul-menu's real gRPC service.
    public bool UseMock { get; set; } = true;
    public string GrpcAddress { get; set; } = "";
}

// Bound from the "Payment" config section.
public sealed class PaymentOptions
{
    public bool UseMock { get; set; } = true;
    public string GrpcAddress { get; set; } = "";

    // Where the frontend should send the customer to pay. The mock payment
    // client builds the redirect URL from this.
    public string PaymentPageUrl { get; set; } = "http://localhost:5176/payment";
}
