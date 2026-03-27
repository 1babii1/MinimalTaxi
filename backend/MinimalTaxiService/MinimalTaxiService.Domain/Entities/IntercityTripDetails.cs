namespace MinimalTaxiService.Domain.Entities;

using CSharpFunctionalExtensions;
using Shared;
using ValueObjects;

public class IntercityTripDetails
{
    public const int MinSeats = 1;
    public const int MaxSeats = 150;

    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Location From { get; private set; } = null!;
    public Location To { get; private set; } = null!;
    public string? FromAddress { get; private set; }
    public string? ToAddress { get; private set; }
    public DateTimeOffset? DepartureAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }
    public int? RequiredSeats { get; private set; }

    public bool IsPassengerRequest => RequiredSeats.HasValue;

    private IntercityTripDetails()
    {
    }

    private IntercityTripDetails(
        Guid tripId,
        Location from,
        Location to,
        string? fromAddress,
        string? toAddress,
        DateTimeOffset departureAt,
        int totalSeats,
        int availableSeats,
        int? requiredSeats)
    {
        Id = Guid.NewGuid();
        TripId = tripId;
        From = from;
        To = to;
        FromAddress = string.IsNullOrWhiteSpace(fromAddress) ? null : fromAddress.Trim();
        ToAddress = string.IsNullOrWhiteSpace(toAddress) ? null : toAddress.Trim();
        DepartureAt = departureAt;
        TotalSeats = totalSeats;
        AvailableSeats = availableSeats;
        RequiredSeats = requiredSeats;
    }

    public static Result<IntercityTripDetails, Error> CreateDriverOffer(
        Guid tripId,
        Location? from,
        Location? to,
        string? fromAddress,
        string? toAddress,
        DateTimeOffset departureAt,
        int totalSeats)
    {
        var errors = new List<ErrorMessages>();

        if (tripId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "Trip id is required", nameof(tripId)));

        if (from is null)
            errors.Add(new ErrorMessages("value.is.required", "From location is required", nameof(from)));

        if (to is null)
            errors.Add(new ErrorMessages("value.is.required", "To location is required", nameof(to)));

        if (totalSeats < MinSeats || totalSeats > MaxSeats)
            errors.Add(new ErrorMessages("value.is.invalid", $"Total seats must be between {MinSeats} and {MaxSeats}", nameof(totalSeats)));

        if (departureAt == default)
            errors.Add(new ErrorMessages("value.is.required", "DepartureAt is required", nameof(departureAt)));

        if (errors.Any())
            return Result.Failure<IntercityTripDetails, Error>(Error.Validation(errors));

        return Result.Success<IntercityTripDetails, Error>(new IntercityTripDetails(tripId, from!, to!, fromAddress, toAddress, departureAt, totalSeats, totalSeats, null));
    }

    public static Result<IntercityTripDetails, Error> CreatePassengerRequest(
        Guid tripId,
        Location? from,
        Location? to,
        string? fromAddress,
        string? toAddress,
        DateTimeOffset departureAt,
        int requiredSeats)
    {
        var errors = new List<ErrorMessages>();

        if (tripId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "Trip id is required", nameof(tripId)));

        if (from is null)
            errors.Add(new ErrorMessages("value.is.required", "From location is required", nameof(from)));

        if (to is null)
            errors.Add(new ErrorMessages("value.is.required", "To location is required", nameof(to)));

        if (requiredSeats < MinSeats || requiredSeats > MaxSeats)
            errors.Add(new ErrorMessages("value.is.invalid", $"Required seats must be between {MinSeats} and {MaxSeats}", nameof(requiredSeats)));

        if (departureAt == default)
            errors.Add(new ErrorMessages("value.is.required", "DepartureAt is required", nameof(departureAt)));

        if (errors.Any())
            return Result.Failure<IntercityTripDetails, Error>(Error.Validation(errors));

        return Result.Success<IntercityTripDetails, Error>(new IntercityTripDetails(tripId, from!, to!, fromAddress, toAddress, departureAt, 0, 0, requiredSeats));
    }

    public bool HasAvailableSeats(int count = 1)
    {
        return AvailableSeats >= count;
    }

    public UnitResult<Error> AssignDriverOffer(int totalSeats)
    {
        if (!IsPassengerRequest)
            return Error.Validation("operation.is.invalid", "Trip details already configured as driver offer");

        if (totalSeats < MinSeats || totalSeats > MaxSeats)
            return Error.Validation("value.is.invalid", $"Total seats must be between {MinSeats} and {MaxSeats}", nameof(totalSeats));

        if (RequiredSeats.HasValue && totalSeats < RequiredSeats.Value)
            return Error.Validation("operation.is.invalid", "Total seats cannot be less than requested seats", nameof(totalSeats));

        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        RequiredSeats = null;

        return Result.Success<Error>();
    }

    public UnitResult<Error> BookSeats(int count)
    {
        if (count < MinSeats || count > MaxSeats)
            return Error.Validation("value.is.invalid", $"Seats count must be between {MinSeats} and {MaxSeats}", nameof(count));

        if (TotalSeats <= 0)
            return Error.Validation("operation.is.invalid", "Seats cannot be booked before driver offer is configured");

        if (count > AvailableSeats)
            return Error.Validation("operation.is.invalid", "Not enough available seats", nameof(count));

        AvailableSeats -= count;

        return Result.Success<Error>();
    }

    public UnitResult<Error> FreeSeats(int count)
    {
        if (count < MinSeats || count > MaxSeats)
            return Error.Validation("value.is.invalid", $"Seats count must be between {MinSeats} and {MaxSeats}", nameof(count));

        if (TotalSeats <= 0)
            return Error.Validation("operation.is.invalid", "Seats cannot be released before driver offer is configured");

        AvailableSeats += count;
        if (AvailableSeats > TotalSeats)
            AvailableSeats = TotalSeats;

        return Result.Success<Error>();
    }
}