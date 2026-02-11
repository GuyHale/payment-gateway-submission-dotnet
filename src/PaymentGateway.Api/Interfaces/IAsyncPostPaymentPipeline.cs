using FluentResults;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Interfaces;

public interface IAsyncPostPaymentPipeline
{
    Task<Result<PostPaymentResponse>> PostAsync(PostPaymentRequest request, CancellationToken ct = default);
}
