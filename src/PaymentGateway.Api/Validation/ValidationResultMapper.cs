using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PaymentGateway.Api.Validation;

public static class ValidationResultMapper
{
    public static ModelStateDictionary ToModelStateDictionary(this ValidationResult validationResult)
    {
        var modelState = new ModelStateDictionary();

        foreach (var failure in validationResult.Errors)
        {
            modelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
        }

        return modelState;
    }
}
