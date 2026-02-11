using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Factories;

public static class PaymentDetailsFactory
{
    public static TPaymentDetails ToPaymentDetails<TPaymentDetails>(this PostPaymentRequest request, Guid id, PaymentStatus status)
        where TPaymentDetails : PaymentDetails, new()
    {
        MaskedCardNumber maskedCardNumber = new(request.CardNumber);

        if (!maskedCardNumber.IsValid)
        {
            throw new ArgumentException("The card number provided was invalid.");
        }

        return new TPaymentDetails()
        {
            CardNumberLastFour = maskedCardNumber.Value,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
            Id = id,
            Status = status,
        };
    }
}
