using MinimalTaxiService.Contracts.Profiles;

namespace MinimalTaxiService.Web.Internal;

public sealed class BootstrapProfileRequest
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public AddressDto? Address { get; set; }
    public CarInfoDto? CarInfo { get; set; }
}
