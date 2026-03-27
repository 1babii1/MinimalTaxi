
using System.Text.Json;
using FluentValidation.Results;
using Shared;

namespace MinimalTaxiService.Application.Validation;

public static class ValidationExtentions
{
    public static Error ToError(this ValidationResult validationResult)
    {
        var errorMessages = new List<ErrorMessages>();

        foreach (var validationFailure in validationResult.Errors)
        {
            var message = validationFailure.ErrorMessage;
            var field = string.IsNullOrWhiteSpace(validationFailure.PropertyName)
                ? "validation"
                : validationFailure.PropertyName;

            try
            {
                var parsed = JsonSerializer.Deserialize<Error>(message);
                if (parsed is not null && parsed.Messages.Any())
                {
                    errorMessages.AddRange(parsed.Messages);
                    continue;
                }
            }
            catch
            {
            }

            errorMessages.Add(new ErrorMessages("validation.error", message, field));
        }

        return Error.Validation(errorMessages);
    }
}