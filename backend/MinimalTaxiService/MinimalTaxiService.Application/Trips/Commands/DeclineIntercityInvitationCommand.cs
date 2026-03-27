using CSharpFunctionalExtensions;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Trips.Invitations;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record DeclineIntercityInvitationCommand(
    Guid InvitationId,
    Guid PassengerId);

public sealed class DeclineIntercityInvitationCommandHandler(
    IIntercityInvitationStore invitationStore,
    ITripEventsBus tripEventsBus)
{
    public async Task<UnitResult<Error>> Handle(DeclineIntercityInvitationCommand command, CancellationToken cancellationToken)
    {
        invitationStore.ExpireStale();

        var invitation = invitationStore.GetById(command.InvitationId);
        if (invitation is null)
            return Error.NotFound("invitation.not_found", "Invitation not found", nameof(command.InvitationId));

        if (invitation.PassengerId != command.PassengerId)
            return Error.Validation("operation.is.forbidden", "Invitation belongs to another passenger");

        if (invitation.Status != IntercityInvitationStatus.Pending)
            return Error.Validation("operation.is.invalid", "Only pending invitation can be declined");

        invitationStore.MarkDeclined(invitation.Id);

        await tripEventsBus.PublishAsync(
            new TripChangedEvent(invitation.PassengerTripId, "invitation.declined", DateTimeOffset.UtcNow),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
