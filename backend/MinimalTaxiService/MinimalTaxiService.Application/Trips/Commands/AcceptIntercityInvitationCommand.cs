using CSharpFunctionalExtensions;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Trips.Invitations;
using MinimalTaxiService.Domain.Services;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record AcceptIntercityInvitationCommand(
    Guid InvitationId,
    Guid PassengerId);

public sealed class AcceptIntercityInvitationCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    IIntercityInvitationStore invitationStore)
{
    public async Task<UnitResult<Error>> Handle(AcceptIntercityInvitationCommand command, CancellationToken cancellationToken)
    {
        invitationStore.ExpireStale();

        var invitation = invitationStore.GetById(command.InvitationId);
        if (invitation is null)
            return Error.NotFound("invitation.not_found", "Invitation not found", nameof(command.InvitationId));

        if (invitation.PassengerId != command.PassengerId)
            return Error.Validation("operation.is.forbidden", "Invitation belongs to another passenger");

        if (invitation.Status != IntercityInvitationStatus.Pending)
            return Error.Validation("operation.is.invalid", "Only pending invitation can be accepted");

        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            invitationStore.MarkExpired(invitation.Id);
            return Error.Validation("operation.is.invalid", "Invitation is expired");
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var passengerTrip = await tripRepository.GetTripByIdWithLock(invitation.PassengerTripId, cancellationToken);
        if (passengerTrip is null)
            return Error.NotFound("trip.not_found", "Passenger trip not found", nameof(invitation.PassengerTripId));

        var driverTrip = await tripRepository.GetTripByIdWithParticipants(invitation.DriverTripId, cancellationToken);
        if (driverTrip is null)
            return Error.NotFound("trip.not_found", "Driver trip not found", nameof(invitation.DriverTripId));

        if (passengerTrip.DriverId.HasValue || passengerTrip.Status != Domain.Enums.TripStatus.Created)
            return Error.Validation("operation.is.invalid", "Passenger trip is no longer available");

        if (driverTrip.Status != Domain.Enums.TripStatus.Created)
            return Error.Validation("operation.is.invalid", "Driver offer is not available");

        var passengerDetails = await tripRepository.GetIntercityDetailsByTripIdWithLock(invitation.PassengerTripId, cancellationToken);
        if (passengerDetails is null)
            return Error.NotFound("trip.intercity_details.not_found", "Passenger intercity details not found");

        var driverDetails = await tripRepository.GetIntercityDetailsByTripIdWithLock(invitation.DriverTripId, cancellationToken);
        if (driverDetails is null)
            return Error.NotFound("trip.intercity_details.not_found", "Driver intercity details not found");

        var requiredSeats = passengerDetails.RequiredSeats ?? 1;
        var seatsAllowed = TripDomainService.EnsureSeatsAvailable(driverDetails, requiredSeats);
        if (seatsAllowed.IsFailure)
            return seatsAllowed.Error;

        await tripRepository.UpsertIntercityParticipant(
            invitation.DriverTripId,
            command.PassengerId,
            requiredSeats,
            passengerDetails.From,
            passengerDetails.To,
            passengerDetails.FromAddress ?? "Точка посадки",
            passengerDetails.ToAddress ?? "Точка высадки",
            cancellationToken);

        var bookResult = driverDetails.BookSeats(requiredSeats);
        if (bookResult.IsFailure)
            return bookResult.Error;

        var acceptResult = passengerTrip.Accept(invitation.DriverId);
        if (acceptResult.IsFailure)
            return acceptResult.Error;

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            scope.Rollback();
            return saveResult.Error;
        }

        var commitResult = scope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        invitationStore.MarkAccepted(invitation.Id);

        await tripEventsBus.PublishAsync(
            new TripChangedEvent(invitation.PassengerTripId, "invitation.accepted", DateTimeOffset.UtcNow),
            cancellationToken);

        await tripEventsBus.PublishAsync(
            new TripChangedEvent(invitation.PassengerTripId, "trip.updated", DateTimeOffset.UtcNow),
            cancellationToken);

        await tripEventsBus.PublishAsync(
            new TripChangedEvent(invitation.DriverTripId, "trip.updated", DateTimeOffset.UtcNow),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
