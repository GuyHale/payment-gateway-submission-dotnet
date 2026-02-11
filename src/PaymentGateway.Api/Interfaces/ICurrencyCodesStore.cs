namespace PaymentGateway.Api.Interfaces;

public interface ICurrencyCodesStore
{
    bool Contains(string currencyCode);
}
