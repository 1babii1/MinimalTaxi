using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Domain.Services;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record AcceptTripCommand(Guid TripId, Guid DriverId, int? TotalSeats = null);

public sealed class AcceptTripValidation : AbstractValidator<AcceptTripCommand>
{
    public AcceptTripValidation()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("TripId is required");

        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("DriverId is required");

        RuleFor(x => x.TotalSeats)
            .GreaterThan(0)
            .WithMessage("TotalSeats must be greater than zero")
            .LessThanOrEqualTo(150)
            .WithMessage("TotalSeats must be less than or equal to 150")
            .When(x => x.TotalSeats.HasValue);
    }
}

public sealed class AcceptTripCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    AcceptTripValidation validator,
    ILogger<AcceptTripCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(AcceptTripCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate accept trip command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var trip = await tripRepository.GetTripByIdWithLock(command.TripId, cancellationToken);
        if (trip is null)
            return Error.NotFound("trip.not_found", "Trip not found", nameof(command.TripId));

        var canAccept = TripDomainService.EnsureTripCanBeAccepted(trip, command.DriverId);
        if (canAccept.IsFailure)
            return canAccept.Error;

        var acceptResult = trip.Accept(command.DriverId);
        if (acceptResult.IsFailure)
            return acceptResult.Error;

        if (trip.Type == Domain.Enums.TripType.Intercity)
        {
            var details = await tripRepository.GetIntercityDetailsByTripIdWithLock(trip.Id, cancellationToken);
            if (details is null)
                return Error.NotFound("trip.intercity_details.not_found", "Intercity trip details not found");

            if (details.IsPassengerRequest)
            {
                if (!command.TotalSeats.HasValue)
                    return Error.Validation("value.is.required", "Total seats is required when accepting passenger intercity request", nameof(command.TotalSeats));

                var assignResult = details.AssignDriverOffer(command.TotalSeats.Value);
                if (assignResult.IsFailure)
                    return assignResult.Error;
            }
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
            new TripChangedEvent(trip.Id, "trip.accepted", DateTimeOffset.UtcNow),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
