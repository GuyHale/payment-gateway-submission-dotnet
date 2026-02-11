using Bogus;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Tests.Helpers;

namespace PaymentGateway.Api.Tests;

public class PostPaymentRequestBuilder
{
    private readonly Faker _faker;

    private string _cardNumber;
    private int _expiryMonth;
    private int _expiryYear;
    private int _amount;
    private string _currency;
    private string _cvv;

    public PostPaymentRequestBuilder(Faker faker, string[] validCurrencyCodes)
    {
        _faker = faker;

        _cardNumber = _faker.ValidCardNumber();

        var expiryDate = _faker.ValidExpiryDate();
        _expiryMonth = expiryDate.Month;
        _expiryYear = expiryDate.Year;

        _amount = _faker.ValidAmount();
        _currency = _faker.ValidCurrencyCode(validCurrencyCodes);
        _cvv = _faker.ValidCvv();
    }

    public PostPaymentRequestBuilder WithCardNumber(string cardNumber)
    {
        _cardNumber = cardNumber;
        return this;
    }

    public PostPaymentRequestBuilder WithExpiryMonth(int expiryMonth)
    {
        _expiryMonth = expiryMonth;
        return this;
    }

    public PostPaymentRequestBuilder WithExpiryYear(int expiryYear)
    {
        _expiryYear = expiryYear;
        return this;
    }

    public PostPaymentRequestBuilder WithAmount(int amount)
    {
        _amount = amount;
        return this;
    }

    public PostPaymentRequestBuilder WithCurrencyCode(string currency)
    {
        _currency = currency;
        return this;
    }

    public PostPaymentRequestBuilder WithCvv(string cvv)
    {
        _cvv = cvv;
        return this;
    }

    public PostPaymentRequest Build()
    {
        return new()
        {
            CardNumber = _cardNumber,
            ExpiryMonth = _expiryMonth,
            ExpiryYear = _expiryYear,
            Amount = _amount,
            Currency = _currency,
            Cvv = _cvv
        };
    }
}
