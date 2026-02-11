using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Configuration;

public class CurrencyCodesConfig
{
    public const string SectionKey = "CurrencyCodesConfig";

    [MinLength(1, ErrorMessage = "At least one currency code must be provided.")]
    public HashSet<string> CurrencyCodes { get; init; } = [];
}
