using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Interfaces
{
    public interface IBankService
    {
        Task<PaymentStatus> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default);
    }
}
