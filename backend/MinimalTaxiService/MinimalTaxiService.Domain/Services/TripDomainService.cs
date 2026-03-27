using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Domain.Services;

using Entities;
using Enums;

public class TripDomainService
{
    public const int MaxActiveTripsPerPassenger = 3;

    public static UnitResult<Error> EnsurePassengerDoesNotExceedActiveTripLimit(
        IEnumerable<Trip> passengerTrips,
        int maxConcurrentTrips = MaxActiveTripsPerPassenger)
    {
        var count = passengerTrips.Count(t => t.IsActive);

        if (count >= maxConcurrentTrips)
        {
            return Error.Conflict(
                "trip.limit.exceeded",
                $"Maximum {maxConcurrentTrips} active trips allowed per passenger");
        }

        return Result.Success<Error>();
    }

    public static UnitResult<Error> EnsurePassengerActiveTripLimit(
        int activeTripCount,
        int maxConcurrentTrips = MaxActiveTripsPerPassenger)
    {
        if (activeTripCount >= maxConcurrentTrips)
        {
            return Error.Conflict(
                "trip.limit.exceeded",
                $"Maximum {maxConcurrentTrips} active trips allowed per passenger");
        }

        return Result.Success<Error>();
    }

    public static UnitResult<Error> EnsureTripCanBeAccepted(Trip trip, Guid driverId)
    {
        if (driverId == Guid.Empty)
            return Error.Validation("value.is.required", "Driver id is required", nameof(driverId));

        if (trip.Status != TripStatus.Created)
            return Error.Validation("operation.is.invalid", $"Trip {trip.Id} cannot be accepted in status {trip.Status}");

        if (trip.DriverId.HasValue)
            return Error.Conflict("trip.already.accepted", $"Trip {trip.Id} already has a driver");

        if (trip.PassengerId == driverId)
            return Error.Validation("operation.is.invalid", "Driver cannot be same as passenger");

        return Result.Success<Error>();
    }

    public static UnitResult<Error> EnsureSeatsAvailable(
        IntercityTripDetails intercityDetails,
        int seatsToBook)
    {
        if (seatsToBook <= 0)
            return Error.Validation("value.is.invalid", "Seats must be greater than zero", nameof(seatsToBook));

        if (!intercityDetails.HasAvailableSeats(seatsToBook))
            return Error.Conflict("trip.seats.not_enough", $"Requested {seatsToBook} seats, available {intercityDetails.AvailableSeats}");

        return Result.Success<Error>();
    }

    public static UnitResult<Error> EnsureIntercityJoinAllowed(Trip trip, Guid passengerId, int seatsToBook)
    {
        if (trip.Type != TripType.Intercity)
            return Error.Validation("operation.is.invalid", "Only intercity trips support participants");

        if (trip.Status is TripStatus.Completed or TripStatus.CancelledByDriver or TripStatus.CancelledByPassenger or TripStatus.Expired)
            return Error.Validation("operation.is.invalid", "Cannot join a finished trip");

        if (passengerId == Guid.Empty)
            return Error.Validation("value.is.required", "Passenger id is required", nameof(passengerId));

        if (seatsToBook <= 0)
            return Error.Validation("value.is.invalid", "Seats must be greater than zero", nameof(seatsToBook));

        if (trip.PassengerId == passengerId)
            return Error.Validation("operation.is.invalid", "Trip owner cannot join own trip");

        if (trip.DriverId == passengerId)
            return Error.Validation("operation.is.invalid", "Trip driver cannot join as passenger");

        return Result.Success<Error>();
    }
}