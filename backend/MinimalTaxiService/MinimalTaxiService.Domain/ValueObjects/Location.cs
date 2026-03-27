using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Domain.ValueObjects;

public record Location
{
    public double Latitude { get; }
    public double Longitude { get; }

    private Location(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Result<Location, Error> Create(double latitude, double longitude)
    {
        var errors = new List<ErrorMessages>();

        if (latitude is < -90 or > 90)
            errors.Add(new ErrorMessages("value.is.invalid", "Latitude must be in range [-90, 90]", nameof(latitude)));

        if (longitude is < -180 or > 180)
            errors.Add(new ErrorMessages("value.is.invalid", "Longitude must be in range [-180, 180]", nameof(longitude)));

        if (errors.Any())
            return Result.Failure<Location, Error>(Error.Validation(errors));

        return Result.Success<Location, Error>(new Location(latitude, longitude));
    }

    public static Location Empty => new(0.0, 0.0);
}