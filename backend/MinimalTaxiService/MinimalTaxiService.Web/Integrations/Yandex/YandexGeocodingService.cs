using System.Text.Json;
using Microsoft.Extensions.Options;
using MinimalTaxiService.Contracts.Geocoding;

namespace MinimalTaxiService.Web.Integrations.Yandex;

public sealed class YandexGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly YandexOptions _options;

    public YandexGeocodingService(HttpClient httpClient, IOptions<YandexOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AddressSuggestionDto>> SuggestAsync(string address, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return [];

        var normalized = address.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return [];

        var boundedLimit = Math.Max(1, Math.Min(limit, 10));

        var queryParams = new Dictionary<string, string>
        {
            ["apikey"] = _options.ApiKey,
            ["geocode"] = normalized,
            ["format"] = "json",
            ["results"] = boundedLimit.ToString(),
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var url = $"https://geocode-maps.yandex.ru/1.x/?{queryString}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var parsed = await JsonSerializer.DeserializeAsync<YandexGeocodeResponse>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        var members = parsed?.Response?.GeoObjectCollection?.FeatureMembers;
        if (members is null || members.Count == 0)
            return [];

        var result = new List<AddressSuggestionDto>(members.Count);

        foreach (var member in members)
        {
            var geoObject = member.GeoObject;
            if (geoObject?.Point?.Pos.TryParseToLocation(out var latitude, out var longitude) != true)
                continue;

            var formattedAddress = geoObject.MetaDataProperty?.GeocoderMetaData?.Address?.Formatted;
            if (string.IsNullOrWhiteSpace(formattedAddress))
                continue;

            result.Add(new AddressSuggestionDto
            {
                Address = formattedAddress,
                Latitude = latitude,
                Longitude = longitude,
            });
        }

        return result;
    }
}
