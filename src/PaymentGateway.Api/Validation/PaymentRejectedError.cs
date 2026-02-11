using FluentResults;

namespace PaymentGateway.Api.Validation;

public class PaymentRejectedError : Error
{
    public PaymentRejectedError() : base("Payment was rejected by the bank.") { }
}
