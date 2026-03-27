using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Profiles.Commands;
using MinimalTaxiService.Application.Profiles.Queries;
using MinimalTaxiService.Contracts.Profiles;
using MinimalTaxiService.Domain.Entities;
using MinimalTaxiService.Web.Extensions;
using MinimalTaxiService.Web.Integrations.Storage;
using Shared;

namespace MinimalTaxiService.Web.Controllers;

[ApiController]
[Route("profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] IFormFile? file,
        [FromServices] SelectelS3StorageService storageService,
        [FromServices] IUserRepository userRepository,
        [FromServices] ITransactionManager transactionManager,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        if (file is null)
            return BadRequest(Envelope.Error(Error.Validation("value.is.required", "Avatar file is required", nameof(file))));

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return this.ToHttpResult(beginTransaction.Error);

        using var scope = beginTransaction.Value;

        var user = await userRepository.GetByIdWithLock(userId.Value, cancellationToken);
        if (user is null)
            return NotFound(Envelope.Error(Error.NotFound("user.not_found", "User profile is not found")));

        string avatarUrl;
        try
        {
            avatarUrl = await storageService.UploadAvatarAsync(file, userId.Value, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            scope.Rollback();
            return BadRequest(Envelope.Error(Error.Validation("avatar.upload.invalid", ex.Message)));
        }
        catch (AmazonS3Exception ex)
        {
            scope.Rollback();
            return BadRequest(Envelope.Error(Error.Validation("avatar.upload.s3", $"S3 upload failed: {ex.Message}")));
        }

        var updateAvatarResult = user.UpdateAvatar(avatarUrl);
        if (updateAvatarResult.IsFailure)
        {
            scope.Rollback();
            return this.ToHttpResult(updateAvatarResult.Error);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            scope.Rollback();
            return this.ToHttpResult(saveResult.Error);
        }

        var commitResult = scope.Commit();
        if (commitResult.IsFailure)
            return this.ToHttpResult(commitResult.Error);

        return Ok(Envelope.Ok(new { avatarUrl }));
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(
        [FromServices] GetProfileQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var profile = await handler.Handle(new GetProfileQuery(userId.Value), cancellationToken);
        if (profile is null)
        {
            var role = User.GetUserRole()?.ToString() ?? string.Empty;
            profile = new ProfileDto
            {
                UserId = userId.Value,
                Name = string.Empty,
                AvatarUrl = null,
                Role = role,
                Phone = null,
                Address = null,
                CarInfo = null
            };
        }

        return Ok(Envelope.Ok(profile));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] UpdateProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var role = User.GetUserRole();
        if (!role.HasValue)
            return Forbid();

        var command = new UpdateProfileCommand(
            userId.Value,
            role.Value,
            request.Name,
            request.Phone,
            request.Address,
            request.CarInfo);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }

    [HttpGet("locations")]
    public async Task<IActionResult> GetSavedLocations(
        [FromServices] GetSavedLocationsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var locations = await handler.Handle(new GetSavedLocationsQuery(userId.Value), cancellationToken);
        return Ok(Envelope.Ok(locations));
    }

    [HttpPost("locations")]
    public async Task<IActionResult> CreateSavedLocation(
        [FromBody] CreateSavedLocationRequest request,
        [FromServices] CreateSavedLocationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(
            new CreateSavedLocationCommand(
                userId.Value,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude),
            cancellationToken);

        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok(result.Value));
    }

    [HttpDelete("locations/{locationId:guid}")]
    public async Task<IActionResult> DeleteSavedLocation(
        [FromRoute] Guid locationId,
        [FromServices] DeleteSavedLocationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new DeleteSavedLocationCommand(userId.Value, locationId), cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }
}
