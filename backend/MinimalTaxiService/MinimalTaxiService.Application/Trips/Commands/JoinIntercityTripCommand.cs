using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Domain.Services;
using MinimalTaxiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record JoinIntercityTripCommand(
    Guid TripId,
    Guid PassengerId,
    int Seats,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string PickupAddress,
    string DropoffAddress);

public sealed class JoinIntercityTripValidation : AbstractValidator<JoinIntercityTripCommand>
{
    public JoinIntercityTripValidation()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("TripId is required");

        RuleFor(x => x.PassengerId)
            .NotEmpty()
            .WithMessage("PassengerId is required");

        RuleFor(x => x.Seats)
            .GreaterThan(0)
            .WithMessage("Seats must be greater than zero")
            .LessThanOrEqualTo(150)
            .WithMessage("Seats must be less than or equal to 150");

        RuleFor(x => x.PickupAddress)
            .NotEmpty().WithMessage("PickupAddress is required")
            .MaximumLength(LenghtConstants.LENGTH150).WithMessage($"PickupAddress max length is {LenghtConstants.LENGTH150}");

        RuleFor(x => x.DropoffAddress)
            .NotEmpty().WithMessage("DropoffAddress is required")
            .MaximumLength(LenghtConstants.LENGTH150).WithMessage($"DropoffAddress max length is {LenghtConstants.LENGTH150}");

        RuleFor(x => x)
            .Must(command => Location.Create(command.PickupLatitude, command.PickupLongitude).IsSuccess)
            .WithMessage("Pickup location is invalid");

        RuleFor(x => x)
            .Must(command => Location.Create(command.DropoffLatitude, command.DropoffLongitude).IsSuccess)
            .WithMessage("Dropoff location is invalid");
    }
}

public sealed class JoinIntercityTripCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    JoinIntercityTripValidation validator,
    ILogger<JoinIntercityTripCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(JoinIntercityTripCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate join intercity trip command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var trip = await tripRepository.GetTripByIdWithParticipants(command.TripId, cancellationToken);
        if (trip is null)
            return Error.NotFound("trip.not_found", "Trip not found", nameof(command.TripId));

        var joinAllowed = TripDomainService.EnsureIntercityJoinAllowed(trip, command.PassengerId, command.Seats);
        if (joinAllowed.IsFailure)
            return joinAllowed.Error;

        var details = await tripRepository.GetIntercityDetailsByTripIdWithLock(command.TripId, cancellationToken);
        if (details is null)
            return Error.NotFound("trip.intercity_details.not_found", "Intercity trip details not found");

        var pickupLocationResult = Location.Create(command.PickupLatitude, command.PickupLongitude);
        if (pickupLocationResult.IsFailure)
            return pickupLocationResult.Error;

        var dropoffLocationResult = Location.Create(command.DropoffLatitude, command.DropoffLongitude);
        if (dropoffLocationResult.IsFailure)
            return dropoffLocationResult.Error;

        var seatsAllowed = TripDomainService.EnsureSeatsAvailable(details, command.Seats);
        if (seatsAllowed.IsFailure)
            return seatsAllowed.Error;

        await tripRepository.UpsertIntercityParticipant(
            command.TripId,
            command.PassengerId,
            command.Seats,
            pickupLocationResult.Value,
            dropoffLocationResult.Value,
            command.PickupAddress,
            command.DropoffAddress,
            cancellationToken);

        var bookResult = details.BookSeats(command.Seats);
        if (bookResult.IsFailure)
            return bookResult.Error;

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

        return UnitResult.Success<Error>();
    }
}
