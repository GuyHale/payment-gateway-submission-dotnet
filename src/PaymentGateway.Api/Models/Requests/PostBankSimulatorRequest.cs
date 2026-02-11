namespace PaymentGateway.Api.Models.Requests;

public record PostBankSimulatorRequest
{
    public string CardNumber { get; init; } = string.Empty;
    public string ExpiryDate { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public int Amount { get; init; }
    public string Cvv { get; init; } = string.Empty;
}
