using CSharpFunctionalExtensions;
using MinimalTaxiService.Domain.ValueObjects;
using Shared;

namespace MinimalTaxiService.Domain.Entities;

public class SavedLocation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public Location Location { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private SavedLocation()
    {
    }

    private SavedLocation(Guid userId, string name, string address, Location location)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        Address = address;
        Location = location;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<SavedLocation, Error> Create(Guid userId, string? name, string? address, double latitude, double longitude)
    {
        var errors = new List<ErrorMessages>();

        var normalizedName = name?.Trim();
        var normalizedAddress = address?.Trim();

        if (userId == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "User id is required", nameof(userId)));

        if (string.IsNullOrWhiteSpace(normalizedName))
            errors.Add(new ErrorMessages("value.is.required", "Location name is required", nameof(name)));
        else if (normalizedName.Length > LenghtConstants.LENGTH120)
            errors.Add(new ErrorMessages("length.is.invalid", $"Location name cannot be greater than {LenghtConstants.LENGTH120} characters", nameof(name)));

        if (string.IsNullOrWhiteSpace(normalizedAddress))
            errors.Add(new ErrorMessages("value.is.required", "Address is required", nameof(address)));
        else if (normalizedAddress.Length > LenghtConstants.LENGTH500)
            errors.Add(new ErrorMessages("length.is.invalid", $"Address cannot be greater than {LenghtConstants.LENGTH500} characters", nameof(address)));

        var locationResult = Location.Create(latitude, longitude);
        if (locationResult.IsFailure)
            errors.AddRange(locationResult.Error.Messages);

        if (errors.Any())
            return Result.Failure<SavedLocation, Error>(Error.Validation(errors));

        return Result.Success<SavedLocation, Error>(
            new SavedLocation(userId, normalizedName!, normalizedAddress!, locationResult.Value));
    }
}