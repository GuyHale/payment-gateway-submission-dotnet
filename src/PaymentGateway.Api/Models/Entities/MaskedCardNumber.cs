namespace PaymentGateway.Api.Models.Entities;

public readonly record struct MaskedCardNumber
{
    private const int MaskedCardNumberLength = 4;

    public MaskedCardNumber(string cardNumber)
    {
        IsValid = !string.IsNullOrWhiteSpace(cardNumber)
            && cardNumber.Length >= MaskedCardNumberLength;

        if(IsValid)
        {
            Value = cardNumber[^MaskedCardNumberLength..];
        }
    }

    public bool IsValid { get; }
    public string Value { get; } = string.Empty;
}
