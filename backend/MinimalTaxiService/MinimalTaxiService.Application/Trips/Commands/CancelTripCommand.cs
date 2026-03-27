using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record CancelTripCommand(Guid TripId, Guid UserId);

public sealed class CancelTripValidation : AbstractValidator<CancelTripCommand>
{
    public CancelTripValidation()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("TripId is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

    }
}

public sealed class CancelTripCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    CancelTripValidation validator,
    ILogger<CancelTripCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(CancelTripCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate cancel trip command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var trip = await tripRepository.GetTripByIdWithLock(command.TripId, cancellationToken);
        if (trip is null)
            return Error.NotFound("trip.not_found", "Trip not found", nameof(command.TripId));

        UnitResult<Error> cancelResult;

        if (trip.DriverId == command.UserId)
        {
            cancelResult = trip.CancelByDriver();
        }
        else if (trip.PassengerId == command.UserId)
        {
            cancelResult = trip.CancelByPassenger();
        }
        else
        {
            return Error.Validation("operation.is.invalid", "Only trip owner can cancel trip");
        }

        if (cancelResult.IsFailure)
            return cancelResult.Error;

        if (
            trip.Type == TripType.Intercity &&
            trip.PassengerId == Guid.Empty &&
            trip.DriverId == command.UserId)
        {
            await tripRepository.ReopenPassengerRequestsLinkedToDriverOffer(
                trip.Id,
                command.UserId,
                cancellationToken);
        }

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
            new TripChangedEvent(trip.Id, "trip.cancelled", DateTimeOffset.UtcNow),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
