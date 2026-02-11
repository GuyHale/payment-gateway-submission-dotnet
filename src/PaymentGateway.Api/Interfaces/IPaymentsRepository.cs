using System.Diagnostics.CodeAnalysis;

using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Interfaces;

public interface IPaymentsRepository
{
    void Add(PaymentDetails paymentDetails);
    bool TryGetValue(Guid id, [NotNullWhen(true)] out PaymentDetails? payment);
}
