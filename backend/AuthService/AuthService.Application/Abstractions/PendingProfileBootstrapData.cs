namespace AuthService.Application.Abstractions;

public sealed class PendingProfileBootstrapData
{
    public string? Name { get; init; }
    public string? Phone { get; init; }
    public PendingProfileAddressData? Address { get; init; }
    public PendingProfileCarInfoData? CarInfo { get; init; }

    public bool HasAnyData() =>
        !string.IsNullOrWhiteSpace(Name) ||
        !string.IsNullOrWhiteSpace(Phone) ||
        Address is not null ||
        CarInfo is not null;
}

public sealed class PendingProfileAddressData
{
    public string? City { get; init; }
    public string? Street { get; init; }
    public string? House { get; init; }
    public string? Apartment { get; init; }
}

public sealed class PendingProfileCarInfoData
{
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string? Color { get; init; }
    public string? PlateNumber { get; init; }
}