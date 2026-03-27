using System.Text.Json.Serialization;

namespace MinimalTaxiService.Web.Integrations.Dadata;

public sealed class DadataReverseResponse
{
    [JsonPropertyName("suggestions")]
    public List<DadataSuggestion>? Suggestions { get; set; }
}

public sealed class DadataSuggestion
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
