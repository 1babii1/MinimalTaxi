namespace MinimalTaxiService.Domain.Entities;

using CSharpFunctionalExtensions;
using Shared;
using ValueObjects;

public class TripParticipant
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsDriver { get; private set; }
    public int BookedSeats { get; private set; }
    public Location? PickupLocation { get; private set; }
    public Location? DropoffLocation { get; private set; }
    public string? PickupAddress { get; private set; }
    public string? DropoffAddress { get; private set; }

    private TripParticipant()
    {
    }

    private TripParticipant(
        Guid tripId,
        Guid userId,
        bool isDriver,
        int bookedSeats,
        Location? pickupLocation,
        Location? dropoffLocation,
        string? pickupAddress,
        string? dropoffAddress)
    {
        Id = Guid.NewGuid();
        TripId = tripId;
        UserId = userId;
        IsDriver = isDriver;
        BookedSeats = bookedSeats;
        PickupLocation = pickupLocation;
        DropoffLocation = dropoffLocation;
        PickupAddress = string.IsNullOrWhiteSpace(pickupAddress) ? null : pickupAddress.Trim();
        DropoffAddress = string.IsNullOrWhiteSpace(dropoffAddress) ? null : dropoffAddress.Trim();
    }

    public static Result<TripParticipant, Error> Create(
        Guid tripId,
        Guid userId,
        bool isDriver,
        int bookedSeats = 1,
        Location? pickupLocation = null,
        Location? dropoffLocation = null,
        string? pickupAddress = null,
        string? dropoffAddress = null)
    {
        var errors = new List<ErrorMessages>();

        if (tripId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "Trip id is required", nameof(tripId)));

        if (userId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "User id is required", nameof(userId)));

        if (isDriver)
        {
            if (bookedSeats != 0)
                errors.Add(new ErrorMessages("value.is.invalid", "Driver booked seats must be equal to zero", nameof(bookedSeats)));
        }
        else
        {
            if (bookedSeats <= 0)
                errors.Add(new ErrorMessages("value.is.invalid", "Booked seats must be greater than zero", nameof(bookedSeats)));

            if (pickupLocation is null)
                errors.Add(new ErrorMessages("value.is.required", "Pickup location is required", nameof(pickupLocation)));

            if (dropoffLocation is null)
                errors.Add(new ErrorMessages("value.is.required", "Dropoff location is required", nameof(dropoffLocation)));

            if (string.IsNullOrWhiteSpace(pickupAddress))
                errors.Add(new ErrorMessages("value.is.required", "Pickup address is required", nameof(pickupAddress)));

            if (string.IsNullOrWhiteSpace(dropoffAddress))
                errors.Add(new ErrorMessages("value.is.required", "Dropoff address is required", nameof(dropoffAddress)));
        }

        if (errors.Any())
            return Result.Failure<TripParticipant, Error>(Error.Validation(errors));

        return Result.Success<TripParticipant, Error>(
            new TripParticipant(
                tripId,
                userId,
                isDriver,
                bookedSeats,
                pickupLocation,
                dropoffLocation,
                pickupAddress,
                dropoffAddress));
    }

    public UnitResult<Error> IncreaseSeats(int count)
    {
        if (IsDriver)
            return Error.Validation("operation.is.invalid", "Driver seats cannot be changed");

        if (count <= 0)
            return Error.Validation("value.is.invalid", "Seats increment must be greater than zero", nameof(count));

        BookedSeats += count;

        return Result.Success<Error>();
    }

    public UnitResult<Error> DecreaseSeats(int count)
    {
        if (IsDriver)
            return Error.Validation("operation.is.invalid", "Driver seats cannot be changed");

        if (count <= 0)
            return Error.Validation("value.is.invalid", "Seats decrement must be greater than zero", nameof(count));

        if (count > BookedSeats)
            return Error.Validation("operation.is.invalid", "Cannot remove more seats than booked", nameof(count));

        BookedSeats -= count;

        return Result.Success<Error>();
    }
}