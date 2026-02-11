using System.Net;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IAsyncPostPaymentPipeline _postPaymentPipeline;
    private readonly IGetPaymentPipeline _getPaymentPipeline;
    private readonly IValidator<PostPaymentRequest> _postPaymentRequestValidator;
    private readonly ILogger<PaymentsController> _logger;

    private const string UndefinedErrorKey = "Undefined";
    private const string PaymentRejectedErrorMessage = "Payment was rejected by the bank.";

    public PaymentsController(IAsyncPostPaymentPipeline postPaymentPipeline,
        IGetPaymentPipeline getPaymentPipeline,
        IValidator<PostPaymentRequest> postPaymentRequestValidator,
        ILogger<PaymentsController> logger)
    {
        _postPaymentPipeline = postPaymentPipeline;
        _getPaymentPipeline = getPaymentPipeline;
        _postPaymentRequestValidator = postPaymentRequestValidator;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(256)]
    public async Task<IActionResult> PostPaymentAsync(PostPaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var fv = _postPaymentRequestValidator.Validate(request);
            if (!fv.IsValid)
            {
                var errors = fv.ToModelStateDictionary();
                return ValidationProblem(errors);
            }

            var result = await _postPaymentPipeline.PostAsync(request, ct);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            if (result.HasError<PaymentRejectedError>())
            {
                ModelState.AddModelError(UndefinedErrorKey, PaymentRejectedErrorMessage);
                return ValidationProblem(ModelState);
            }

            return ServerError();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing payment for masked card number: {CardNumber}", new MaskedCardNumber(request.CardNumber).Value);
            return ServerError();
        }
    }

    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetPaymentAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = _getPaymentPipeline.Get(id, ct);

            if (result.IsSuccess)
            {
                return result.Value is not null
                    ? Ok(result.Value)
                    : NotFound();
            }

            return ServerError();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving payment for id: {Id}", id);
            return ServerError();
        }
    }

    private ObjectResult ServerError()
    {
        return Problem("Internal server error", statusCode: (int)HttpStatusCode.InternalServerError);
    }
}