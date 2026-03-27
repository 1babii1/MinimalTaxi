using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Domain.Entities;

using Enums;
using ValueObjects;

public class Trip
{
    private readonly List<TripParticipant> _participants = [];

    public Guid Id { get; private set; }
    public Guid PassengerId { get; private set; }
    public Guid? DriverId { get; private set; }

    public TripType Type { get; private set; }
    public TripStatus Status { get; private set; }
    public Location Origin { get; private set; } = null!;
    public Location? Destination { get; private set; }
    public string? FromAddress { get; private set; }
    public string? ToAddress { get; private set; }

    public string? City { get; private set; }
    public string? Description { get; private set; }

    public IReadOnlyCollection<TripParticipant> Participants => _participants.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public bool IsActive => Status is TripStatus.Created or TripStatus.DriverAccepted;

    private Trip()
    {
    }

    public static Result<Trip, Error> CreateLocal(
        Guid passengerId,
        Location? origin,
        Location? destination,
        string? fromAddress,
        string? toAddress,
        string? description)
    {
        var errors = new List<ErrorMessages>();
        var normalizedFromAddress = fromAddress?.Trim();
        var normalizedToAddress = toAddress?.Trim();
        var normalizedDescription = description?.Trim();

        if (passengerId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "Passenger id is required", nameof(passengerId)));

        if (origin is null)
            errors.Add(new ErrorMessages("value.is.required", "Origin is required", nameof(origin)));

        if (destination is null)
            errors.Add(new ErrorMessages("value.is.required", "Destination is required", nameof(destination)));

        if (string.IsNullOrWhiteSpace(normalizedFromAddress))
            errors.Add(new ErrorMessages("value.is.required", "FromAddress is required", nameof(fromAddress)));
        else if (normalizedFromAddress.Length > LenghtConstants.LENGTH150)
            errors.Add(new ErrorMessages("length.is.invalid", $"FromAddress cannot be greater than {LenghtConstants.LENGTH150} characters", nameof(fromAddress)));

        if (string.IsNullOrWhiteSpace(normalizedToAddress))
            errors.Add(new ErrorMessages("value.is.required", "ToAddress is required", nameof(toAddress)));
        else if (normalizedToAddress.Length > LenghtConstants.LENGTH150)
            errors.Add(new ErrorMessages("length.is.invalid", $"ToAddress cannot be greater than {LenghtConstants.LENGTH150} characters", nameof(toAddress)));

        if (normalizedDescription is not null && normalizedDescription.Length > LenghtConstants.LENGTH500)
            errors.Add(new ErrorMessages("length.is.invalid", $"Description cannot be greater than {LenghtConstants.LENGTH500} characters", nameof(description)));

        if (errors.Any())
            return Result.Failure<Trip, Error>(Error.Validation(errors));

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            PassengerId = passengerId,
            DriverId = null,
            Type = TripType.Local,
            Status = TripStatus.Created,
            Origin = origin!,
            Destination = destination,
            FromAddress = normalizedFromAddress,
            ToAddress = normalizedToAddress,
            City = null,
            Description = normalizedDescription,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        return Result.Success<Trip, Error>(trip);
    }

    public static Result<Trip, Error> CreateIntercityByDriver(
        Guid driverId,
        Location? from,
        Location? to,
        string? description)
    {
        var errors = new List<ErrorMessages>();
        var normalizedDescription = description?.Trim();

        if (driverId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "Driver id is required", nameof(driverId)));

        if (from is null)
            errors.Add(new ErrorMessages("value.is.required", "From location is required", nameof(from)));

        if (to is null)
            errors.Add(new ErrorMessages("value.is.required", "To location is required", nameof(to)));

        if (normalizedDescription is not null && normalizedDescription.Length > LenghtConstants.LENGTH500)
            errors.Add(new ErrorMessages("length.is.invalid", $"Description cannot be greater than {LenghtConstants.LENGTH500} characters", nameof(description)));

        if (errors.Any())
            return Result.Failure<Trip, Error>(Error.Validation(errors));

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            PassengerId = Guid.Empty,
            DriverId = driverId,
            Type = TripType.Intercity,
            Status = TripStatus.Created,
            Origin = from!,
            Destination = to!,
            FromAddress = null,
            ToAddress = null,
            City = null,
            Description = normalizedDescription,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        var driverParticipant = TripParticipant.Create(trip.Id, driverId, isDriver: true, bookedSeats: 0).Value;
        trip._participants.Add(driverParticipant);

        return Result.Success<Trip, Error>(trip);
    }

    public static Result<Trip, Error> CreateIntercityRequestByPassenger(
        Guid passengerId,
        Location? from,
        Location? to,
        string? description)
    {
        var errors = new List<ErrorMessages>();
        var normalizedDescription = description?.Trim();

        if (passengerId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "Passenger id is required", nameof(passengerId)));

        if (from is null)
            errors.Add(new ErrorMessages("value.is.required", "From location is required", nameof(from)));

        if (to is null)
            errors.Add(new ErrorMessages("value.is.required", "To location is required", nameof(to)));

        if (normalizedDescription is not null && normalizedDescription.Length > LenghtConstants.LENGTH500)
            errors.Add(new ErrorMessages("length.is.invalid", $"Description cannot be greater than {LenghtConstants.LENGTH500} characters", nameof(description)));

        if (errors.Any())
            return Result.Failure<Trip, Error>(Error.Validation(errors));

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            PassengerId = passengerId,
            DriverId = null,
            Type = TripType.Intercity,
            Status = TripStatus.Created,
            Origin = from!,
            Destination = to!,
            FromAddress = null,
            ToAddress = null,
            City = null,
            Description = normalizedDescription,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        return Result.Success<Trip, Error>(trip);
    }

    public UnitResult<Error> Accept(Guid driverId)
    {
        if (driverId == Guid.Empty)
            return Error.Validation("value.is.required", "Driver id is required", nameof(driverId));

        if (Status != TripStatus.Created)
            return Error.Validation("operation.is.invalid", $"Trip status is {Status}, cannot accept");

        if (Type == TripType.Local)
        {
            if (PassengerId == driverId)
                return Error.Validation("operation.is.invalid", "Driver cannot be the same as passenger");

            if (DriverId.HasValue)
                return Error.Validation("operation.is.invalid", "Trip already has driver");

            DriverId = driverId;
            Status = TripStatus.DriverAccepted;
            UpdatedAt = DateTimeOffset.UtcNow;

            return Result.Success<Error>();
        }

        if (PassengerId == Guid.Empty)
            return Error.Validation("operation.is.invalid", "Driver-created intercity trip cannot be accepted");

        if (PassengerId == driverId)
            return Error.Validation("operation.is.invalid", "Driver cannot be the same as passenger");

        if (DriverId.HasValue)
            return Error.Validation("operation.is.invalid", "Trip already has driver");

        DriverId = driverId;
        Status = TripStatus.DriverAccepted;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> Complete()
    {
        if (Status != TripStatus.DriverAccepted)
            return Error.Validation("operation.is.invalid", "Trip must be in DriverAccepted status");

        Status = TripStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> CancelByPassenger()
    {
        if (Status == TripStatus.Completed)
            return Error.Validation("operation.is.invalid", "Completed trip cannot be cancelled");

        if (Status is TripStatus.CancelledByPassenger or TripStatus.CancelledByDriver or TripStatus.Expired)
            return Error.Validation("operation.is.invalid", "Trip is already finished or cancelled");

        Status = TripStatus.CancelledByPassenger;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> CancelByDriver()
    {
        if (Status == TripStatus.Completed)
            return Error.Validation("operation.is.invalid", "Completed trip cannot be cancelled");

        if (Status is TripStatus.CancelledByPassenger or TripStatus.CancelledByDriver or TripStatus.Expired)
            return Error.Validation("operation.is.invalid", "Trip is already finished or cancelled");

        Status = TripStatus.CancelledByDriver;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> Expire()
    {
        if (Status != TripStatus.Created)
            return Error.Validation("operation.is.invalid", "Only CREATED trips can expire");

        Status = TripStatus.Expired;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> AddIntercityParticipant(
        Guid userId,
        int bookedSeats,
        Location? pickupLocation,
        Location? dropoffLocation,
        string? pickupAddress,
        string? dropoffAddress)
    {
        if (Type != TripType.Intercity)
            return Error.Validation("operation.is.invalid", "Participants can be added only for intercity trips");

        if (Status != TripStatus.Created && Status != TripStatus.DriverAccepted)
            return Error.Validation("operation.is.invalid", "Cannot add participants for finished trips");

        if (userId == Guid.Empty)
            return Error.Validation("value.is.required", "User id is required", nameof(userId));

        if (bookedSeats <= 0)
            return Error.Validation("value.is.invalid", "Booked seats must be greater than zero", nameof(bookedSeats));

        if (PassengerId == userId)
            return Error.Validation("operation.is.invalid", "Passenger owner cannot join own trip");

        if (DriverId == userId)
            return Error.Validation("operation.is.invalid", "Driver is already in trip participants");

        var existing = _participants.FirstOrDefault(p => !p.IsDriver && p.UserId == userId);
        if (existing is not null)
            return existing.IncreaseSeats(bookedSeats);

        var participantResult = TripParticipant.Create(
            Id,
            userId,
            isDriver: false,
            bookedSeats,
            pickupLocation,
            dropoffLocation,
            pickupAddress,
            dropoffAddress);
        if (participantResult.IsFailure)
            return participantResult.Error;

        _participants.Add(participantResult.Value);
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> RemoveIntercityParticipant(Guid userId, int seatsToRemove = 1)
    {
        if (Type != TripType.Intercity)
            return Error.Validation("operation.is.invalid", "Participants can be removed only for intercity trips");

        if (userId == Guid.Empty)
            return Error.Validation("value.is.required", "User id is required", nameof(userId));

        if (seatsToRemove <= 0)
            return Error.Validation("value.is.invalid", "Seats to remove must be greater than zero", nameof(seatsToRemove));

        var existing = _participants.FirstOrDefault(p => !p.IsDriver && p.UserId == userId);
        if (existing is null)
            return Error.NotFound("participant.not_found", "Participant not found", nameof(userId));

        var decreaseResult = existing.DecreaseSeats(seatsToRemove);
        if (decreaseResult.IsFailure)
            return decreaseResult.Error;

        if (existing.BookedSeats == 0)
            _participants.Remove(existing);

        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }
}
