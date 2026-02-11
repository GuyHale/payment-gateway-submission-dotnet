using System.Text.Json;

using FluentResults;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Factories;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Services;

public class PaymentsPipeline : IAsyncPostPaymentPipeline, IGetPaymentPipeline
{
    private readonly IBankService _bankService;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly ILogger<PaymentsPipeline> _logger;

    public PaymentsPipeline(IBankService bankService,
        IPaymentsRepository paymentsRepository,
        ILogger<PaymentsPipeline> logger)
    {
        _bankService = bankService;
        _paymentsRepository = paymentsRepository;
        _logger = logger;
    }

    public Result<GetPaymentResponse?> Get(Guid id, CancellationToken ct = default)
    {
        try
        {
            GetPaymentResponse? response = null;

            if (_paymentsRepository.TryGetValue(id, out var payment))
            {
                response = ToGetPaymentResponse(payment);
            }

            return Result.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment for id: {Id}", id);
            return Result.Fail<GetPaymentResponse?>("Internal server error.");
        }
    }

    public async Task<Result<PostPaymentResponse>> PostAsync(PostPaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var paymentId = Guid.NewGuid();
            PaymentStatus status = await _bankService.ProcessPaymentAsync(request, ct);

            var paymentDetails = request.ToPaymentDetails<PaymentDetails>(paymentId, status);

            if (status is PaymentStatus.None)
            {
                return Result.Fail<PostPaymentResponse>("Internal server error.");
            }

            if (status is PaymentStatus.Rejected)
            {
                _logger.LogWarning("Payment rejected by bank for attempted payment: {Payment}", JsonSerializer.Serialize(paymentDetails));
                return Result.Fail<PostPaymentResponse>(new PaymentRejectedError());
            }

            var paymentResponse = request.ToPaymentDetails<PostPaymentResponse>(paymentId, status);

            _paymentsRepository.Add(paymentDetails);

            return Result.Ok(paymentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for masked card number: {CardNumber}", new MaskedCardNumber(request.CardNumber).Value);
            return Result.Fail<PostPaymentResponse>("Internal server error");
        }
    }

    private static GetPaymentResponse ToGetPaymentResponse(PaymentDetails paymentDetails)
    {
        return new GetPaymentResponse()
        {
            CardNumberLastFour = paymentDetails.CardNumberLastFour,
            ExpiryMonth = paymentDetails.ExpiryMonth,
            ExpiryYear = paymentDetails.ExpiryYear,
            Currency = paymentDetails.Currency,
            Amount = paymentDetails.Amount,
            Id = paymentDetails.Id,
            Status = paymentDetails.Status,
        };
    }
}
