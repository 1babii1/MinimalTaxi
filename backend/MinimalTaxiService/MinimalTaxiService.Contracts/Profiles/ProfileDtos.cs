namespace MinimalTaxiService.Contracts.Profiles;

public sealed class ProfileDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public CarInfoDto? CarInfo { get; set; }
}

public sealed class AddressDto
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string House { get; set; } = string.Empty;
    public string? Apartment { get; set; }
}

public sealed class CarInfoDto
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public AddressDto? Address { get; set; }
    public CarInfoDto? CarInfo { get; set; }
}
