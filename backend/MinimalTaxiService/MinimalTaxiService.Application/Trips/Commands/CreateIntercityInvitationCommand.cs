using CSharpFunctionalExtensions;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Trips.Invitations;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record CreateIntercityInvitationCommand(
    Guid DriverId,
    Guid PassengerTripId,
    Guid DriverTripId);

public sealed class CreateIntercityInvitationCommandHandler(
    ITripRepository tripRepository,
    IIntercityInvitationStore invitationStore,
    ITripEventsBus tripEventsBus)
{
    public async Task<Result<Guid, Error>> Handle(CreateIntercityInvitationCommand command, CancellationToken cancellationToken)
    {
        invitationStore.ExpireStale();

        var passengerTrip = await tripRepository.GetTripByIdWithLock(command.PassengerTripId, cancellationToken);
        if (passengerTrip is null)
            return Error.NotFound("trip.not_found", "Passenger trip not found", nameof(command.PassengerTripId));

        if (passengerTrip.PassengerId == Guid.Empty)
            return Error.Validation("operation.is.invalid", "Passenger trip must be created by passenger");

        if (passengerTrip.DriverId.HasValue || passengerTrip.Status != Domain.Enums.TripStatus.Created)
            return Error.Validation("operation.is.invalid", "Passenger trip is no longer available for invitations");

        var driverTrip = await tripRepository.GetTripByIdWithLock(command.DriverTripId, cancellationToken);
        if (driverTrip is null)
            return Error.NotFound("trip.not_found", "Driver trip not found", nameof(command.DriverTripId));

        if (driverTrip.DriverId != command.DriverId)
            return Error.Validation("operation.is.forbidden", "Driver can invite only from own intercity offer");

        if (driverTrip.PassengerId != Guid.Empty)
            return Error.Validation("operation.is.invalid", "Driver trip must be a driver-created offer");

        if (driverTrip.Status != Domain.Enums.TripStatus.Created)
            return Error.Validation("operation.is.invalid", "Driver offer must be in created status");

        var driverDetails = await tripRepository.GetIntercityDetailsByTripIdWithLock(command.DriverTripId, cancellationToken);
        if (driverDetails is null)
            return Error.NotFound("trip.intercity_details.not_found", "Driver intercity details not found");

        var passengerDetails = await tripRepository.GetIntercityDetailsByTripIdWithLock(command.PassengerTripId, cancellationToken);
        if (passengerDetails is null)
            return Error.NotFound("trip.intercity_details.not_found", "Passenger intercity details not found");

        var requiredSeats = passengerDetails.RequiredSeats ?? 1;
        if (!driverDetails.HasAvailableSeats(requiredSeats))
            return Error.Conflict("trip.seats.not_enough", "Not enough seats for invitation");

        var hasAccepted = invitationStore.GetForPassengerTrip(command.PassengerTripId)
            .Any(invite => invite.Status == IntercityInvitationStatus.Accepted);
        if (hasAccepted)
            return Error.Conflict("invitation.already.accepted", "Passenger trip already has accepted invitation");

        var duplicatePending = invitationStore.GetForPassengerTrip(command.PassengerTripId)
            .Any(invite =>
                invite.DriverTripId == command.DriverTripId &&
                invite.Status == IntercityInvitationStatus.Pending &&
                invite.ExpiresAt > DateTimeOffset.UtcNow);
        if (duplicatePending)
            return Error.Conflict("invitation.already.exists", "Pending invitation already exists for this driver offer");

        var invitation = invitationStore.Add(
            command.PassengerTripId,
            command.DriverTripId,
            passengerTrip.PassengerId,
            command.DriverId);

        await tripEventsBus.PublishAsync(
            new TripChangedEvent(command.PassengerTripId, "invitation.created", DateTimeOffset.UtcNow),
            cancellationToken);

        return invitation.Id;
    }
}
