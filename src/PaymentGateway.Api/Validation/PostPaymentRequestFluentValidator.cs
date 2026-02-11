using FluentValidation;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validation;

public sealed class PostPaymentRequestFluentValidator : AbstractValidator<PostPaymentRequest>
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrencyCodesStore _currencyCodesStore;

    private const int MinCardNumberLength = 14,
        MaxCardNumberLength = 19,
        MinMonth = 1,
        MaxMonth = 12,
        CurrencyLength = 3,
        MinAmount = 1,
        MinCvvLength = 3,
        MaxCvvLength = 4;

    public PostPaymentRequestFluentValidator(
        IDateTimeProvider dateTimeProvider,
        ICurrencyCodesStore currencyCodesStore)
    {
        _dateTimeProvider = dateTimeProvider;
        _currencyCodesStore = currencyCodesStore;

        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .Length(MinCardNumberLength, MaxCardNumberLength)
            .Must(BeDigits).WithMessage("CardNumber must contain numerical characters only.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(MinMonth, MaxMonth);

        RuleFor(x => x)
            .Must(x => DateIsInFuture(x.ExpiryMonth, x.ExpiryYear))
            .WithName("ExpiryDate")
            .WithMessage("ExpiryMonth and ExpiryYear must be in the future.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(CurrencyLength)
            .Must(IsCurrencyCode).WithMessage("Currency was not recognised.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(MinAmount);

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .Length(MinCvvLength, MaxCvvLength)
            .Must(BeDigits).WithMessage("Cvv must contain numerical characters only.");
    }

    private static bool BeDigits(string text) => text.All(char.IsDigit);

    private bool IsCurrencyCode(string text) => _currencyCodesStore.Contains(text);

    private bool DateIsInFuture(int month, int year)
    {
        var now = _dateTimeProvider.UtcNow;
        return year > now.Year || (year == now.Year && month > now.Month);
    }
}