using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Domain.Entities;
using MinimalTaxiService.Domain.Services;
using MinimalTaxiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Commands;

public sealed record CreateIntercityTripCommand(
    Guid UserId,
    bool CreatedByDriver,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude,
    string? FromAddress,
    string? ToAddress,
    DateTimeOffset DepartureAt,
    string? Description,
    int? TotalSeats,
    int? RequiredSeats);

public sealed class CreateIntercityTripValidation : AbstractValidator<CreateIntercityTripCommand>
{
    public CreateIntercityTripValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Description)
            .MaximumLength(LenghtConstants.LENGTH500)
            .WithMessage($"Description max length is {LenghtConstants.LENGTH500}")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x)
            .Must(command => Location.Create(command.FromLatitude, command.FromLongitude).IsSuccess)
            .WithMessage("From location is invalid");

        RuleFor(x => x)
            .Must(command => Location.Create(command.ToLatitude, command.ToLongitude).IsSuccess)
            .WithMessage("To location is invalid");

        RuleFor(x => x.FromAddress)
            .NotEmpty().WithMessage("FromAddress is required for intercity")
            .MaximumLength(LenghtConstants.LENGTH150)
            .WithMessage($"FromAddress max length is {LenghtConstants.LENGTH150}");

        RuleFor(x => x.ToAddress)
            .NotEmpty().WithMessage("ToAddress is required for intercity")
            .MaximumLength(LenghtConstants.LENGTH150)
            .WithMessage($"ToAddress max length is {LenghtConstants.LENGTH150}");

        RuleFor(x => x.TotalSeats)
            .NotNull().WithMessage("TotalSeats is required for driver-created intercity trip")
            .GreaterThan(0).WithMessage("TotalSeats must be greater than zero")
            .LessThanOrEqualTo(150).WithMessage("TotalSeats must be less than or equal to 150")
            .When(x => x.CreatedByDriver);

        RuleFor(x => x.RequiredSeats)
            .NotNull().WithMessage("RequiredSeats is required for passenger-created intercity request")
            .GreaterThan(0).WithMessage("RequiredSeats must be greater than zero")
            .LessThanOrEqualTo(150).WithMessage("RequiredSeats must be less than or equal to 150")
            .When(x => !x.CreatedByDriver);

        RuleFor(x => x.DepartureAt)
            .Must(value => value != default)
            .WithMessage("DepartureAt is required")
            .Must(value => value >= DateTimeOffset.UtcNow.AddMinutes(-1))
            .WithMessage("DepartureAt must not be in the past");
    }
}

public sealed class CreateIntercityTripCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    CreateIntercityTripValidation validator,
    ILogger<CreateIntercityTripCommandHandler> logger)
{
    public async Task<Result<Guid, Error>> Handle(CreateIntercityTripCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate create intercity trip command");
            return validateResult.ToError();
        }

        var fromResult = Location.Create(command.FromLatitude, command.FromLongitude);
        if (fromResult.IsFailure)
            return fromResult.Error;

        var toResult = Location.Create(command.ToLatitude, command.ToLongitude);
        if (toResult.IsFailure)
            return toResult.Error;

        Result<Trip, Error> tripResult;
        Result<IntercityTripDetails, Error> detailsResult;

        if (command.CreatedByDriver)
        {
            if (!command.TotalSeats.HasValue)
                return Error.Validation("value.is.required", "Total seats is required for driver-created intercity trip", nameof(command.TotalSeats));

            tripResult = Trip.CreateIntercityByDriver(command.UserId, fromResult.Value, toResult.Value, command.Description);
            if (tripResult.IsFailure)
                return tripResult.Error;

            detailsResult = IntercityTripDetails.CreateDriverOffer(
                tripResult.Value.Id,
                fromResult.Value,
                toResult.Value,
                command.FromAddress,
                command.ToAddress,
                command.DepartureAt,
                command.TotalSeats.Value);
            if (detailsResult.IsFailure)
                return detailsResult.Error;
        }
        else
        {
            var activeCount = await tripRepository.GetActiveTripCountForPassenger(command.UserId, cancellationToken);
            var limitResult = TripDomainService.EnsurePassengerActiveTripLimit(activeCount, TripDomainService.MaxActiveTripsPerPassenger);
            if (limitResult.IsFailure)
                return limitResult.Error;

            if (!command.RequiredSeats.HasValue)
                return Error.Validation("value.is.required", "Required seats is required for passenger-created intercity request", nameof(command.RequiredSeats));

            tripResult = Trip.CreateIntercityRequestByPassenger(command.UserId, fromResult.Value, toResult.Value, command.Description);
            if (tripResult.IsFailure)
                return tripResult.Error;

            detailsResult = IntercityTripDetails.CreatePassengerRequest(
                tripResult.Value.Id,
                fromResult.Value,
                toResult.Value,
                command.FromAddress,
                command.ToAddress,
                command.DepartureAt,
                command.RequiredSeats.Value);
            if (detailsResult.IsFailure)
                return detailsResult.Error;
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        await tripRepository.AddTrip(tripResult.Value, cancellationToken);
        await tripRepository.AddIntercityDetails(detailsResult.Value, cancellationToken);

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
            new TripChangedEvent(tripResult.Value.Id, "trip.created", DateTimeOffset.UtcNow),
            cancellationToken);

        return tripResult.Value.Id;
    }
}
