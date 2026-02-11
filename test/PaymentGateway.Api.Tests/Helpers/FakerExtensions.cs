using Bogus;
using Bogus.Extensions.Extras;

namespace PaymentGateway.Api.Tests.Helpers;

public static class FakerExtensions
{
    public static DateOnly ValidExpiryDate(this Faker faker) => faker.Date.FutureDateOnly();
    public static string ValidCardNumber(this Faker faker) => faker.Finance.CreditCardNumber()
            .Replace("-", string.Empty);
    public static string ValidMaskedCardNumber(this Faker faker) => faker.Finance.CreditCardNumberLastFourDigits();
    public static string ValidCvv(this Faker faker) => faker.Finance.CreditCardCvv();
    public static int ValidAmount(this Faker faker) => faker.Random.Int(1, int.MaxValue);
    public static string ValidCurrencyCode(this Faker faker, string[] validCodes) => faker.PickRandom(validCodes);
}
