using MinimalTaxiService.Domain.ValueObjects;
using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Domain.Entities;

using Enums;

public class User
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public UserAddress? Address { get; private set; }

    public UserRole Role { get; private set; }
    public CarInfo? CarInfo { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private User()
    {
    }

    private User(Guid id, string displayName, UserRole role, string? phoneNumber = null, UserAddress? address = null, CarInfo? carInfo = null, string? avatarUrl = null)
    {
        Id = id;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        PhoneNumber = phoneNumber;
        Address = address;
        Role = role;
        CarInfo = carInfo;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = null;
    }

    public static Result<User, Error> Create(
        Guid id,
        string? displayName,
        UserRole role,
        string? phoneNumber = null,
        UserAddress? address = null,
        CarInfo? carInfo = null,
        string? avatarUrl = null)
    {
        var errors = new List<ErrorMessages>();
        var normalizedDisplayName = displayName?.Trim();
        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);

        if (id == Guid.Empty)
            errors.Add(new ErrorMessages("value.is.required", "User id is required", nameof(id)));

        if (string.IsNullOrWhiteSpace(normalizedDisplayName) || normalizedDisplayName.Length < LenghtConstants.LENGTH2)
            errors.Add(new ErrorMessages("length.is.invalid", $"Display name cannot be less than {LenghtConstants.LENGTH2} characters", nameof(displayName)));
        else if (normalizedDisplayName.Length > LenghtConstants.LENGTH100)
            errors.Add(new ErrorMessages("length.is.invalid", $"Display name cannot be greater than {LenghtConstants.LENGTH100} characters", nameof(displayName)));

        if (!string.IsNullOrWhiteSpace(phoneNumber) && normalizedPhoneNumber is null)
            errors.Add(new ErrorMessages("value.is.invalid", "Phone number must match format +7XXXXXXXXXX", nameof(phoneNumber)));

        if (role == UserRole.Passenger && carInfo is not null)
            errors.Add(new ErrorMessages("operation.is.invalid", "Passenger cannot have car info", nameof(carInfo)));

        if (errors.Any())
            return Result.Failure<User, Error>(Error.Validation(errors));

        return Result.Success<User, Error>(new User(
            id,
            normalizedDisplayName!,
            role,
            normalizedPhoneNumber,
            address,
            carInfo,
            avatarUrl));
    }

    public UnitResult<Error> UpdateProfile(string? displayName, string? phoneNumber)
    {
        var normalizedDisplayName = displayName?.Trim();
        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);

        if (string.IsNullOrWhiteSpace(normalizedDisplayName) || normalizedDisplayName.Length < LenghtConstants.LENGTH2)
            return Error.Validation("length.is.invalid", $"Display name cannot be less than {LenghtConstants.LENGTH2} characters", nameof(displayName));

        if (normalizedDisplayName.Length > LenghtConstants.LENGTH100)
            return Error.Validation("length.is.invalid", $"Display name cannot be greater than {LenghtConstants.LENGTH100} characters", nameof(displayName));

        if (!string.IsNullOrWhiteSpace(phoneNumber) && normalizedPhoneNumber is null)
            return Error.Validation("value.is.invalid", "Phone number must match format +7XXXXXXXXXX", nameof(phoneNumber));

        DisplayName = normalizedDisplayName;
        PhoneNumber = normalizedPhoneNumber;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    private static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        string normalizedDigits;

        if (digits.Length == 11 && digits[0] == '8')
            normalizedDigits = $"7{digits[1..]}";
        else if (digits.Length == 11 && digits[0] == '7')
            normalizedDigits = digits;
        else if (digits.Length == 10 && digits[0] == '9')
            normalizedDigits = $"7{digits}";
        else
            return null;

        return normalizedDigits.Length == 11 && normalizedDigits[0] == '7'
            ? $"+{normalizedDigits}"
            : null;
    }

    public UnitResult<Error> UpdateAddress(UserAddress? address)
    {
        Address = address;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> AssignCarInfo(CarInfo? carInfo)
    {
        if (Role != UserRole.Driver && carInfo is not null)
            return Error.Validation("operation.is.invalid", "Only driver can have car info", nameof(carInfo));

        CarInfo = carInfo;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }

    public UnitResult<Error> UpdateAvatar(string? avatarUrl)
    {
        if (avatarUrl is not null && avatarUrl.Length > LenghtConstants.LENGTH500)
            return Error.Validation("length.is.invalid", $"AvatarUrl cannot be greater than {LenghtConstants.LENGTH500} characters", nameof(avatarUrl));

        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success<Error>();
    }
}