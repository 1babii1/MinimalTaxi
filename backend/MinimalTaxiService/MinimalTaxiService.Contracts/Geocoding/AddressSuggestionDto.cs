namespace MinimalTaxiService.Contracts.Geocoding;

public sealed class AddressSuggestionDto
{
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
