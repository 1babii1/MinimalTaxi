using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Domain.ValueObjects;

public record UserAddress
{
    public string City { get; }
    public string Street { get; }
    public string House { get; }
    public string? Apartment { get; }

    private UserAddress(string city, string street, string house, string? apartment)
    {
        City = city;
        Street = street;
        House = house;
        Apartment = apartment;
    }

    public static Result<UserAddress, Error> Create(string city, string street, string house, string? apartment = null)
    {
        var errors = new List<ErrorMessages>();

        var normalizedCity = city?.Trim();
        var normalizedStreet = street?.Trim();
        var normalizedHouse = house?.Trim();
        var normalizedApartment = apartment?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCity) || normalizedCity.Length < LenghtConstants.LENGTH2)
            errors.Add(new ErrorMessages("length.is.invalid", $"City cannot be less than {LenghtConstants.LENGTH2} characters", nameof(city)));
        else if (normalizedCity.Length > LenghtConstants.LENGTH100)
            errors.Add(new ErrorMessages("length.is.invalid", $"City cannot be greater than {LenghtConstants.LENGTH100} characters", nameof(city)));

        if (string.IsNullOrWhiteSpace(normalizedStreet) || normalizedStreet.Length < LenghtConstants.LENGTH2)
            errors.Add(new ErrorMessages("length.is.invalid", $"Street cannot be less than {LenghtConstants.LENGTH2} characters", nameof(street)));
        else if (normalizedStreet.Length > LenghtConstants.LENGTH150)
            errors.Add(new ErrorMessages("length.is.invalid", $"Street cannot be greater than {LenghtConstants.LENGTH150} characters", nameof(street)));

        if (string.IsNullOrWhiteSpace(normalizedHouse))
            errors.Add(new ErrorMessages("value.is.required", "House is required", nameof(house)));
        else if (normalizedHouse.Length > LenghtConstants.LENGTH20)
            errors.Add(new ErrorMessages("length.is.invalid", $"House cannot be greater than {LenghtConstants.LENGTH20} characters", nameof(house)));

        if (normalizedApartment is not null && normalizedApartment.Length > LenghtConstants.LENGTH20)
            errors.Add(new ErrorMessages("length.is.invalid", $"Apartment cannot be greater than {LenghtConstants.LENGTH20} characters", nameof(apartment)));

        if (errors.Any())
            return Result.Failure<UserAddress, Error>(Error.Validation(errors));

        return Result.Success<UserAddress, Error>(new UserAddress(
            normalizedCity!,
            normalizedStreet!,
            normalizedHouse!,
            normalizedApartment));
    }
}