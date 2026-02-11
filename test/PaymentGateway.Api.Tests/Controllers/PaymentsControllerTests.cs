using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Bogus;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Tests.Helpers;

namespace PaymentGateway.Api.Tests.Controllers;

public class PaymentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fixture;
    private readonly string[] _validCurrencyCodes;
    private static readonly PaymentStatus[] ValidPaymentStatuses;
    private readonly Faker _faker;

    public PaymentsControllerTests(WebApplicationFactory<Program> fixture)
    {
        _fixture = fixture;

        _validCurrencyCodes = _fixture.Services
            .GetRequiredService<IOptionsMonitor<CurrencyCodesConfig>>().CurrentValue.CurrencyCodes
            .ToArray();

        _faker = new Faker
        {
            Random = new Randomizer(123)
        };
    }

    static PaymentsControllerTests()
    {
        ValidPaymentStatuses = [PaymentStatus.Authorized, PaymentStatus.Declined];
    }

    [Fact]
    public async Task Get_WhenPaymentExists_ThenReturnsOkAndPayment()
    {
        // Arrange
        var expected = PostPaymentResponse();

        var paymentsRepository = GetService<IPaymentsRepository>();
        paymentsRepository.Add(expected);

        var client = GetClientForTesting();

        // Act
        var response = await GetPaymentAsync(client, expected.Id);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equivalent(expected, paymentResponse);
    }

    [Fact]
    public async Task Get_WhenPaymentDoesNotExist_ThenReturnsNotFoundProblemDetails()
    {
        // Arrange

        var client = GetClientForTesting();

        // Act
        var response = await GetPaymentAsync(client, Guid.NewGuid());
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal((int)HttpStatusCode.NotFound, problemDetails.Status);
    }

    [Theory]
    [InlineData(null, "field is required.")]
    [InlineData("", "must not be empty.")]
    [InlineData("11111", "between 14 and 19 characters.")]
    [InlineData("11111111111111111111", "between 14 and 19 characters.")]
    [InlineData("AAAAAAAAAAAAAA", "contain numerical characters only.")]
    public async Task Post_WhenCardNumberInvalid_ThenReturnsBadRequestProblemDetailsWithRequestError(string? cardNumber,
        string expectedErrorMessageSlice)
    {
        // Arrange

        const string expectedErrorKey = "CardNumber";

        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCardNumber(cardNumber!)
            .Build();

        // Act && Assert
        await InvalidPostPaymentRequestActAndAssertAsync(request, expectedErrorKey, expectedErrorMessageSlice);
    }

    [Theory]
    [InlineData(2018, 7, "ExpiryDate", "must be in the future.")]
    [InlineData(9999, 15, "ExpiryMonth", "between 1 and 12")]
    public async Task Post_WhenExpiryDateInvalid_ThenReturnsBadRequestProblemDetailsWithRequestError(int expiryYear,
        int expiryMonth,
        string expectedErrorKey,
        string expectedErrorMessageSlice)
    {
        // Arrange
        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithExpiryMonth(expiryMonth)
            .WithExpiryYear(expiryYear)
            .Build();

        // Act && Assert
        await InvalidPostPaymentRequestActAndAssertAsync(request, expectedErrorKey, expectedErrorMessageSlice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-5_000)]
    public async Task Post_WhenAmountInvalid_ThenReturnsBadRequestProblemDetailsWithRequestError(int amount)
    {
        // Arrange
        const string expectedErrorKey = "Amount",
            expectedErrorMessageSlice = "greater than or equal to '1'.";
        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithAmount(amount)
            .Build();

        // Act && Assert
        await InvalidPostPaymentRequestActAndAssertAsync(request, expectedErrorKey, expectedErrorMessageSlice);
    }

    [Theory]
    [InlineData(null, "field is required.")]
    [InlineData("", "must not be empty.")]
    [InlineData("XYZ", "was not recognised.")]
    public async Task Post_WhenCurrencyInvalid_ThenReturnsBadRequestProblemDetailsWithRequestError(
        string? currency,
        string expectedErrorMessageSlice)
    {
        // Arrange
        const string expectedErrorKey = "Currency";
        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCurrencyCode(currency!)
            .Build();

        // Act && Assert
        await InvalidPostPaymentRequestActAndAssertAsync(request, expectedErrorKey, expectedErrorMessageSlice);
    }

    [Theory]
    [InlineData(null, "field is required.")]
    [InlineData("", "must not be empty.")]
    [InlineData("12", "between 3 and 4 characters.")]
    [InlineData("12345", "between 3 and 4 characters.")]
    [InlineData("12A", "contain numerical characters only.")]
    public async Task Post_WhenCvvInvalid_ThenReturnsBadRequestProblemDetailsWithRequestError(
        string? cvv,
        string expectedErrorMessageSlice)
    {
        // Arrange
        const string expectedErrorKey = "Cvv";
        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCvv(cvv!)
            .Build();

        // Act && Assert
        await InvalidPostPaymentRequestActAndAssertAsync(request, expectedErrorKey, expectedErrorMessageSlice);
    }

    [Fact]
    public async Task Post_WhenBankSimulatorAccepts_ThenReturnsOkAndPaymentAndStoresPayment()
    {
        // Arrange

        (var cardNumber, var expectedMaskedCardNumber) = AcceptedCardNumber();

        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCardNumber(cardNumber)
            .Build();

        var client = GetClientForTesting();

        var paymentRepository = GetService<IPaymentsRepository>();

        // Act
        var response = await PostPaymentAsync(client, request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AssertPostPaymentResponse(paymentResponse, PaymentStatus.Authorized, expectedMaskedCardNumber, request);

        Assert.True(paymentRepository.TryGetValue(paymentResponse!.Id, out var _));
    }

    [Fact]
    public async Task Post_WhenBankSimulatorDeclines_ThenReturnsOkAndPaymentAndStoresPayment()
    {
        // Arrange

        (var cardNumber, var expectedMaskedCardNumber) = DeclinedCardNumber();

        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCardNumber(cardNumber)
            .Build();

        var client = GetClientForTesting();

        var paymentRepository = GetService<IPaymentsRepository>();

        // Act
        var response = await PostPaymentAsync(client, request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AssertPostPaymentResponse(paymentResponse, PaymentStatus.Declined, expectedMaskedCardNumber, request);

        Assert.True(paymentRepository.TryGetValue(paymentResponse!.Id, out var _));
    }

    [Fact]
    public async Task Post_WhenBankSimulatorErrors_ThenReturnsErrorProblemDetails()
    {
        // Arrange
        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCardNumber(ErrorCardNumber())
            .Build();

        var client = GetClientForTesting();

        // Act
        var response = await PostPaymentAsync(client, request);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problemDetails.Status);
    }

    [Theory]
    // Payment accepted
    [InlineData("333333333333333")]
    // Payment declined
    [InlineData("3333333333333368")]
    public async Task PostAndGet_WhenBankAcceptsOrDeclinesPayment_ThenPostResponseIsEquivalentToGetResponse(string cardNumber)
    {
        // Arrange
        var request = new PostPaymentRequestBuilder(_faker, _validCurrencyCodes)
            .WithCardNumber(cardNumber)
            .Build();

        var client = GetClientForTesting();

        // Act
        var postResponse = await PostPaymentAsync(client, request);
        var postPaymentResponse = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        var getResponse = await GetPaymentAsync(client, postPaymentResponse!.Id);
        var getPaymentResponse = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();

        // Assert

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        GetAndPostResponsesAreEquivalent(getPaymentResponse, postPaymentResponse);
    }

    private HttpClient GetClientForTesting() => _fixture.CreateClient();
    private TService GetService<TService>() where TService : class
        => _fixture.Services.GetRequiredService<TService>();

    private async Task InvalidPostPaymentRequestActAndAssertAsync(PostPaymentRequest request, string expectedErrorKey, string expectedErrorMessageSlice)
    {
        var client = GetClientForTesting();

        // Act
        var response = await PostPaymentAsync(client, request);
        var actual = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetailsError(actual, expectedErrorKey, expectedErrorMessageSlice);
    }

    /// <summary>
    /// Generates a PostPaymentResponse instance with valid property values.
    /// </summary>
    private PostPaymentResponse PostPaymentResponse()
    {
        var expiryDate = _faker.ValidExpiryDate();

        return new PostPaymentResponse
        {
            Id = _faker.Random.Guid(),
            Status = _faker.PickRandom(ValidPaymentStatuses),
            ExpiryYear = expiryDate.Year,
            ExpiryMonth = expiryDate.Month,
            Amount = _faker.ValidAmount(),
            CardNumberLastFour = _faker.ValidMaskedCardNumber(),
            Currency = _faker.ValidCurrencyCode(_validCurrencyCodes),
        };
    }

    private static void AssertPostPaymentResponse(PostPaymentResponse? paymentResponse,
        PaymentStatus expectedStatus,
        string expectedCardNumber,
        PostPaymentRequest request)
    {
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(default, paymentResponse!.Id);
        Assert.Equal(expectedStatus, paymentResponse.Status);
        Assert.Equal(expectedCardNumber, paymentResponse.CardNumberLastFour);
        Assert.Equal(request.Currency, paymentResponse.Currency);
        Assert.Equal(request.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(request.Amount, paymentResponse.Amount);
    }

    private static void GetAndPostResponsesAreEquivalent(GetPaymentResponse? get, PostPaymentResponse? post)
    {
        Assert.NotNull(post);
        Assert.NotNull(get);
        Assert.Equal(get.Id, post.Id);
        Assert.Equal(get.Status, post.Status);
        Assert.Equal(get.CardNumberLastFour, post.CardNumberLastFour);
        Assert.Equal(get.Currency, post.Currency);
        Assert.Equal(get.ExpiryMonth, post.ExpiryMonth);
        Assert.Equal(get.ExpiryYear, post.ExpiryYear);
        Assert.Equal(get.Amount, post.Amount);
    }

    private static void AssertProblemDetailsError(ProblemDetails? actual, string key, string expectedValueSlice)
    {
        Assert.NotNull(actual?.Extensions);
        Assert.Contains("errors", actual.Extensions);

        var errors = actual.Extensions["errors"];

        Assert.NotNull(errors);

        var json = errors.ToString();

        Assert.NotNull(json);

        var errorsDictionary = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json);

        Assert.NotNull(errorsDictionary);
        Assert.Contains(key, (IDictionary<string, string[]>)errorsDictionary);
        Assert.Contains(expectedValueSlice, errorsDictionary[key].First());
    }

    private static (string CardNumber, string Masked) DeclinedCardNumber() => ("11111111111112", "1112");
    private static (string CardNumber, string Masked) AcceptedCardNumber() => ("11111111111111", "1111");
    private static string ErrorCardNumber() => "11111111111110";

    private static async Task<HttpResponseMessage> PostPaymentAsync(HttpClient client, PostPaymentRequest request)
        => await client.PostAsJsonAsync("api/Payments", request);

    private static async Task<HttpResponseMessage> GetPaymentAsync(HttpClient client, Guid id)
        => await client.GetAsync($"/api/Payments/{id}");
}