using System.Diagnostics.CodeAnalysis;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services;

public class InMemoryPaymentsRepository : IPaymentsRepository
{
    private readonly Dictionary<Guid, PaymentDetails> _payments = [];

    public void Add(PaymentDetails paymentDetails)
    {
        _payments[paymentDetails.Id] = paymentDetails;
    }

    public bool TryGetValue(Guid id, [NotNullWhen(true)] out PaymentDetails? payment)
    {
        return _payments.TryGetValue(id, out payment);
    }
}