using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Validation;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record CompleteTripCommand(Guid TripId, Guid DriverId);

public sealed class CompleteTripValidation : AbstractValidator<CompleteTripCommand>
{
    public CompleteTripValidation()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("TripId is required");

        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("DriverId is required");
    }
}

public sealed class CompleteTripCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    CompleteTripValidation validator,
    ILogger<CompleteTripCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(CompleteTripCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate complete trip command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var trip = await tripRepository.GetTripByIdWithLock(command.TripId, cancellationToken);
        if (trip is null)
            return Error.NotFound("trip.not_found", "Trip not found", nameof(command.TripId));

        if (trip.DriverId != command.DriverId)
            return Error.Validation("operation.is.invalid", "Only assigned driver can complete trip");

        var completeResult = trip.Complete();
        if (completeResult.IsFailure)
            return completeResult.Error;

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
            new TripChangedEvent(trip.Id, "trip.completed", DateTimeOffset.UtcNow),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
