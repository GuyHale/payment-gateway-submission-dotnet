using FluentValidation.TestHelper;

using NSubstitute;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Tests.Validation;

public class PostPaymentRequestFluentValidatorTests
{
    [Theory]
    // Valid because request year is greater than current year (month irrelevant)
    [InlineData(2, 2025, 1, 2026)]
    [InlineData(7, 2028, 4, 2032)]
    [InlineData(6, 2031, 6, 2034)]
    // Valid because same year but request month is greater than current month
    [InlineData(2, 2026, 3, 2026)]
    [InlineData(11, 2035, 12, 2035)]
    [InlineData(6, 2039, 8, 2039)]
    public void ExpiryDate_IsValid_ForExpiryDatesInTheFuture(
        int month,
        int year,
        int expiryMonth,
        int expiryYear)
    {
        // Arrange
        DateTime dateTime = MonthYearUtcDateTime(month, year);
        var validator = CreateValidator(dateTime);

        var request = Request(expiryMonth, expiryYear);

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("ExpiryDate");
    }

    [Theory]
    // Invalid because request year is less than current year (month irrelevant)
    [InlineData(2, 2026, 12, 2025)]
    [InlineData(12, 2029, 1, 2028)]
    [InlineData(4, 2027, 9, 2022)]
    // Invalid because same year and request month is equal to current month
    [InlineData(2, 2025, 2, 2025)]
    [InlineData(3, 2024, 3, 2024)]
    [InlineData(10, 2028, 10, 2028)]
    // Invalid because same year and request month is less than current month
    [InlineData(2, 2030, 1, 2030)]
    [InlineData(5, 2031, 2, 2031)]
    [InlineData(8, 2036, 6, 2036)]
    public void ExpiryDate_IsInvalid_ForExpiryDatesNotInTheFuture(
        int month,
        int year,
        int expiryMonth,
        int expiryYear)
    {
        // Arrange
        DateTime dateTime = MonthYearUtcDateTime(month, year);
        var validator = CreateValidator(dateTime);

        var request = Request(expiryMonth, expiryYear);

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("ExpiryDate")
            .WithErrorMessage("ExpiryMonth and ExpiryYear must be in the future.");
    }

    private static PostPaymentRequest Request(int expiryMonth, int expiryYear) => new()
    {
        CardNumber = "4242424242424242",
        ExpiryMonth = expiryMonth,
        ExpiryYear = expiryYear,
        Currency = "GBP",
        Amount = 123,
        Cvv = "123"
    };

    private static PostPaymentRequestFluentValidator CreateValidator(DateTime utcNow)
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(utcNow);

        var currencyCodesStore = Substitute.For<ICurrencyCodesStore>();
        currencyCodesStore.Contains("GBP").Returns(true);

        return new PostPaymentRequestFluentValidator(dateTimeProvider, currencyCodesStore);
    }

    private static DateTime MonthYearUtcDateTime(int month, int year)
        => new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
}