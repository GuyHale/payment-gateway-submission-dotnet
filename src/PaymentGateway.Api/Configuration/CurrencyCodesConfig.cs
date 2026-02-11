namespace PaymentGateway.Api.Configuration;

public class CurrencyCodesConfig
{
    public const string SectionKey = "CurrencyCodesConfig";
    public HashSet<string> CurrencyCodes { get; init; } = [];
}
