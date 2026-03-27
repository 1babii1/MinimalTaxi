using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalTaxiService.Application.Profiles.Commands;
using MinimalTaxiService.Domain.Enums;
using MinimalTaxiService.Web.Extensions;
using MinimalTaxiService.Web.Internal;
using Shared;

namespace MinimalTaxiService.Web.Controllers;

[ApiController]
[Route("internal/profile")]
public class InternalProfileController : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapProfile(
        [FromBody] BootstrapProfileRequest request,
        [FromServices] UpdateProfileCommandHandler handler,
        [FromServices] IOptions<InternalApiOptions> options,
        CancellationToken cancellationToken)
    {
        var providedKey = Request.Headers["X-Internal-Key"].ToString();
        var configuredKey = options.Value.Key;

        if (string.IsNullOrWhiteSpace(configuredKey)
            || string.IsNullOrWhiteSpace(providedKey)
            || !string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
        {
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "Internal key is invalid")));
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest(Envelope.Error(Error.Validation("role.invalid", "Role is invalid", nameof(request.Role))));

        var command = new UpdateProfileCommand(
            request.UserId,
            role,
            request.Name,
            request.Phone,
            request.Address,
            request.CarInfo);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }
}
