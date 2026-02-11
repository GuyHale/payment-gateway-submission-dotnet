using Microsoft.Extensions.Options;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Interfaces;

namespace PaymentGateway.Api.Services;

public class AppConfigDerivedCurrencyCodesStore : ICurrencyCodesStore
{
    private readonly IOptionsMonitor<CurrencyCodesConfig> _currencyCodesConfigMonitor;

    public AppConfigDerivedCurrencyCodesStore(IOptionsMonitor<CurrencyCodesConfig> currencyCodesConfigMonitor)
    {
        _currencyCodesConfigMonitor = currencyCodesConfigMonitor;
    }

    public bool Contains(string currencyCode)
        => _currencyCodesConfigMonitor.CurrentValue.CurrencyCodes.Contains(currencyCode);
}
