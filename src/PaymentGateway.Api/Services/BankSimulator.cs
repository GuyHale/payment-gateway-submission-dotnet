using System.Net;
using System.Text.Json;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class BankSimulator : IBankService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BankSimulator> _logger;

    public BankSimulator(IHttpClientFactory httpClientFactory, ILogger<BankSimulator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<PaymentStatus> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            PostBankSimulatorRequest bankSimRequest = new()
            {
                CardNumber = request.CardNumber,
                ExpiryDate = $"{request.ExpiryMonth:00}/{request.ExpiryYear}",
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = request.Cvv
            };

            var client = _httpClientFactory.CreateClient(nameof(HttpClientType.BankSimulator));

            var response = await client.PostAsJsonAsync("payments", bankSimRequest, Options, cancellationToken);

            if (response.StatusCode is HttpStatusCode.BadRequest)
            {
                return PaymentStatus.Rejected;
            }

            response.EnsureSuccessStatusCode();

            var bankSimResponse = await response.Content.ReadFromJsonAsync<PostBankSimulatorResponse>(cancellationToken);

            if (bankSimResponse is null)
            {
                _logger.LogWarning("BankSimulator response could not be deserialised, response {Response}, for masked card number: {CardNumber}",
                    JsonSerializer.Serialize(response), new MaskedCardNumber(request.CardNumber).Value);
                return PaymentStatus.None;
            }

            return bankSimResponse.Authorized
                ? PaymentStatus.Authorized
                : PaymentStatus.Declined;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing payment with bank simulator for masked card number: {CardNumber}", new MaskedCardNumber(request.CardNumber).Value);
            return PaymentStatus.None;
        }
    }
}
