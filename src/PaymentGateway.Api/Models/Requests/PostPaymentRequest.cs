namespace PaymentGateway.Api.Models.Requests;

public record PostPaymentRequest
{
    public string CardNumber { get; init; } = string.Empty;
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int Amount { get; init; }
    public string Cvv { get; init; } = string.Empty;
}