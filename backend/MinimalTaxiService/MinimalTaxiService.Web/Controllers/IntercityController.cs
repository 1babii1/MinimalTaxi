using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Commands;
using MinimalTaxiService.Application.Trips.Queries;
using MinimalTaxiService.Contracts.Trips;
using MinimalTaxiService.Web.Extensions;
using Shared;

namespace MinimalTaxiService.Web.Controllers;

[ApiController]
[Route("intercity")]
[Authorize]
public class IntercityController : ControllerBase
{
    [HttpGet("{id:guid}/passengers")]
    public async Task<IActionResult> Passengers(
        [FromRoute] Guid id,
        [FromServices] ITripReadRepository readRepository,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var passengers = await readRepository.GetIntercityPassengersForDriver(id, userId.Value, cancellationToken);
        return Ok(Envelope.Ok(passengers));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(
        [FromRoute] Guid id,
        [FromBody] JoinIntercityTripRequest request,
        [FromServices] JoinIntercityTripCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(
            new JoinIntercityTripCommand(
                id,
                userId.Value,
                request.Seats,
                request.Pickup.Latitude,
                request.Pickup.Longitude,
                request.Dropoff.Latitude,
                request.Dropoff.Longitude,
                request.PickupAddress,
                request.DropoffAddress),
            cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }

    [HttpPost("{id:guid}/participants/{participantUserId:guid}/remove")]
    public async Task<IActionResult> RemoveParticipant(
        [FromRoute] Guid id,
        [FromRoute] Guid participantUserId,
        [FromServices] RemoveIntercityParticipantCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var actorUserId = User.GetUserId();
        if (!actorUserId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(
            new RemoveIntercityParticipantCommand(id, actorUserId.Value, participantUserId),
            cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(
        [FromRoute] Guid id,
        [FromServices] RemoveIntercityParticipantCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var actorUserId = User.GetUserId();
        if (!actorUserId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(
            new RemoveIntercityParticipantCommand(id, actorUserId.Value, actorUserId.Value),
            cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> CreateInvitation(
        [FromBody] CreateIntercityInvitationRequest request,
        [FromServices] CreateIntercityInvitationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var userRole = User.GetUserRole();
        if (!userRole.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User role claim is missing")));

        if (userRole.Value != Domain.Enums.UserRole.Driver)
            return BadRequest(Envelope.Error(Error.Validation("operation.is.forbidden", "Only driver can create invitations")));

        var result = await handler.Handle(
            new CreateIntercityInvitationCommand(userId.Value, request.PassengerTripId, request.DriverTripId),
            cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok(result.Value));
    }

    [HttpGet("invitations/my")]
    public async Task<IActionResult> MyInvitations(
        [FromServices] GetPassengerIntercityInvitationsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new GetPassengerIntercityInvitationsQuery(userId.Value), cancellationToken);

        var response = result.Select(item => new IntercityInvitationDto
        {
            InvitationId = item.InvitationId,
            PassengerTripId = item.PassengerTripId,
            DriverTripId = item.DriverTripId,
            DriverId = item.DriverId,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            ExpiresAt = item.ExpiresAt,
        }).ToList();

        return Ok(Envelope.Ok(response));
    }

    [HttpPost("invitations/{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptInvitation(
        [FromRoute] Guid invitationId,
        [FromServices] AcceptIntercityInvitationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new AcceptIntercityInvitationCommand(invitationId, userId.Value), cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }

    [HttpPost("invitations/{invitationId:guid}/decline")]
    public async Task<IActionResult> DeclineInvitation(
        [FromRoute] Guid invitationId,
        [FromServices] DeclineIntercityInvitationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new DeclineIntercityInvitationCommand(invitationId, userId.Value), cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }
}
