using FluentResults;

using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Interfaces;

public interface IGetPaymentPipeline
{
    Result<GetPaymentResponse?> Get(Guid id, CancellationToken ct = default);
}
