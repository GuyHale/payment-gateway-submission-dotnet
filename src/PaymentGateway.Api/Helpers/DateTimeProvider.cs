using PaymentGateway.Api.Interfaces;

namespace PaymentGateway.Api.Helpers;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
