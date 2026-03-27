using Microsoft.AspNetCore.Mvc;
using Shared;

namespace MinimalTaxiService.Web.Extensions;

public static class ErrorHttpMapper
{
    public static IActionResult ToHttpResult(this ControllerBase controller, Error error)
    {
        var envelope = Envelope.Error(error);

        return error.Type switch
        {
            ErrorType.VALIDATION => controller.BadRequest(envelope),
            ErrorType.NOT_FOUND => controller.NotFound(envelope),
            ErrorType.CONFLICT => controller.Conflict(envelope),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, envelope)
        };
    }
}
