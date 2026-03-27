using MinimalTaxiService.Domain.ValueObjects;
using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Domain.Entities;

public class DriverLocation
{
    public Guid Id { get; private set; }
    public Guid DriverId { get; private set; }
    public Location Location { get; private set; } = null!;
    public DateTimeOffset Timestamp { get; private set; }

    private DriverLocation()
    {
    }

    private DriverLocation(Guid driverId, Location location)
    {
        Id = Guid.NewGuid();
        DriverId = driverId;
        Location = location;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public static Result<DriverLocation, Error> Create(Guid driverId, double latitude, double longitude)
    {
        if (driverId == Guid.Empty)
            return Error.Validation("value.is.required", "Driver id is required", nameof(driverId));

        var locationResult = Location.Create(latitude, longitude);
        if (locationResult.IsFailure)
            return locationResult.Error;

        return Result.Success<DriverLocation, Error>(new DriverLocation(driverId, locationResult.Value));
    }

    public UnitResult<Error> UpdateLocation(double latitude, double longitude)
    {
        var locationResult = Location.Create(latitude, longitude);
        if (locationResult.IsFailure)
            return locationResult.Error;

        Location = locationResult.Value;
        Timestamp = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }
}