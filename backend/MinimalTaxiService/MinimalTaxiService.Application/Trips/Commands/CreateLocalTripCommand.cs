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

public sealed record CreateLocalTripCommand(
    Guid PassengerId,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude,
    string FromAddress,
    string ToAddress,
    string? Description);

public sealed class CreateLocalTripValidation : AbstractValidator<CreateLocalTripCommand>
{
    public CreateLocalTripValidation()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty()
            .WithMessage("PassengerId is required");

        RuleFor(x => x.FromAddress)
            .NotEmpty().WithMessage("FromAddress is required")
            .MaximumLength(LenghtConstants.LENGTH150).WithMessage($"FromAddress max length is {LenghtConstants.LENGTH150}");

        RuleFor(x => x.ToAddress)
            .NotEmpty().WithMessage("ToAddress is required")
            .MaximumLength(LenghtConstants.LENGTH150).WithMessage($"ToAddress max length is {LenghtConstants.LENGTH150}");

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
    }
}

public sealed class CreateLocalTripCommandHandler(
    ITripRepository tripRepository,
    ITransactionManager transactionManager,
    ITripEventsBus tripEventsBus,
    CreateLocalTripValidation validator,
    ILogger<CreateLocalTripCommandHandler> logger)
{
    public async Task<Result<Guid, Error>> Handle(CreateLocalTripCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate create local trip command");
            return validateResult.ToError();
        }

        var activeCount = await tripRepository.GetActiveTripCountForPassenger(command.PassengerId, cancellationToken);
        var limitResult = TripDomainService.EnsurePassengerActiveTripLimit(
            activeCount,
            TripDomainService.MaxActiveTripsPerPassenger);
        if (limitResult.IsFailure)
            return limitResult.Error;

        var fromLocationResult = Location.Create(command.FromLatitude, command.FromLongitude);
        if (fromLocationResult.IsFailure)
            return fromLocationResult.Error;

        var toLocationResult = Location.Create(command.ToLatitude, command.ToLongitude);
        if (toLocationResult.IsFailure)
            return toLocationResult.Error;

        var tripResult = Trip.CreateLocal(
            command.PassengerId,
            fromLocationResult.Value,
            toLocationResult.Value,
            command.FromAddress,
            command.ToAddress,
            command.Description);
        if (tripResult.IsFailure)
            return tripResult.Error;

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        await tripRepository.AddTrip(tripResult.Value, cancellationToken);
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
