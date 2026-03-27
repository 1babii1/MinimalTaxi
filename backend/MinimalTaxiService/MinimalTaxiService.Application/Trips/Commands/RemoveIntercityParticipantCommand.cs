using CSharpFunctionalExtensions;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record RemoveIntercityParticipantCommand(
    Guid TripId,
    Guid ActorUserId,
    Guid ParticipantUserId);

public sealed class RemoveIntercityParticipantCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    ILogger<RemoveIntercityParticipantCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(RemoveIntercityParticipantCommand command, CancellationToken cancellationToken)
    {
        if (command.TripId == Guid.Empty)
            return Error.Validation("value.is.required", "TripId is required", nameof(command.TripId));

        if (command.ActorUserId == Guid.Empty)
            return Error.Validation("value.is.required", "ActorUserId is required", nameof(command.ActorUserId));

        if (command.ParticipantUserId == Guid.Empty)
            return Error.Validation("value.is.required", "ParticipantUserId is required", nameof(command.ParticipantUserId));

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var trip = await tripRepository.GetTripByIdWithParticipants(command.TripId, cancellationToken);
        if (trip is null)
            return Error.NotFound("trip.not_found", "Trip not found", nameof(command.TripId));

        if (trip.DriverId != command.ActorUserId && command.ActorUserId != command.ParticipantUserId)
            return Error.Validation("operation.is.forbidden", "Only trip driver or participant can remove participant");

        if (trip.DriverId == command.ParticipantUserId)
            return Error.Validation("operation.is.invalid", "Driver cannot be removed from participants");

        var participant = trip.Participants.FirstOrDefault(participant =>
            !participant.IsDriver && participant.UserId == command.ParticipantUserId);

        if (participant is null)
            return Error.NotFound("trip.participant.not_found", "Trip participant not found", nameof(command.ParticipantUserId));

        var details = await tripRepository.GetIntercityDetailsByTripIdWithLock(command.TripId, cancellationToken);
        if (details is null)
            return Error.NotFound("trip.intercity_details.not_found", "Intercity trip details not found");

        var removeResult = trip.RemoveIntercityParticipant(command.ParticipantUserId, participant.BookedSeats);
        if (removeResult.IsFailure)
            return removeResult.Error;

        var freeSeatsResult = details.FreeSeats(participant.BookedSeats);
        if (freeSeatsResult.IsFailure)
            return freeSeatsResult.Error;

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            scope.Rollback();
            return saveResult.Error;
        }

        var commitResult = scope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        await tripEventsBus.PublishAsync(
            new TripChangedEvent(trip.Id, "trip.updated", DateTimeOffset.UtcNow),
            cancellationToken);

        logger.LogInformation(
            "Removed intercity participant {ParticipantUserId} from trip {TripId} by actor {ActorUserId}",
            command.ParticipantUserId,
            command.TripId,
            command.ActorUserId);

        return UnitResult.Success<Error>();
    }
}