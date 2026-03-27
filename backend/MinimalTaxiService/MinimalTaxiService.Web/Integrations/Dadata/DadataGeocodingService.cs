using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MinimalTaxiService.Web.Integrations.Dadata;

public sealed class DadataGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly DadataOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public DadataGeocodingService(HttpClient httpClient, IOptions<DadataOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
            return null;

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://suggestions.dadata.ru/suggestions/api/4_1/rs/geolocate/address");

        request.Headers.TryAddWithoutValidation("Authorization", $"Token {_options.ApiKey}");
        request.Headers.TryAddWithoutValidation("X-Secret", _options.SecretKey);

        var payload = JsonSerializer.Serialize(new
        {
            lat = latitude,
            lon = longitude,
            count = 1,
        });

        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var parsed = await JsonSerializer.DeserializeAsync<DadataReverseResponse>(
            stream,
            JsonOptions,
            cancellationToken);

        return parsed?.Suggestions?.FirstOrDefault()?.Value;
    }

    public async Task<IReadOnlyList<string>> SuggestCarBrandsAsync(string query, int limit, CancellationToken cancellationToken)
    {
        return await SuggestAsync(
            "https://suggestions.dadata.ru/suggestions/api/4_1/rs/suggest/car_brand",
            new
            {
                query,
                count = Math.Clamp(limit, 1, 20),
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> SuggestCarModelsAsync(
        string brand,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        return await SuggestAsync(
            "https://suggestions.dadata.ru/suggestions/api/4_1/rs/suggest/car_model",
            new
            {
                query,
                count = Math.Clamp(limit, 1, 20),
                filters = new[]
                {
                    new
                    {
                        brand,
                    },
                },
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<string>> SuggestAsync(
        string url,
        object payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
            return Array.Empty<string>();

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {_options.ApiKey}");
        request.Headers.TryAddWithoutValidation("X-Secret", _options.SecretKey);

        var body = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<string>();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var parsed = await JsonSerializer.DeserializeAsync<DadataReverseResponse>(
            stream,
            JsonOptions,
            cancellationToken);

        return parsed?.Suggestions?
            .Select(item => item.Value?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray() ?? Array.Empty<string>();
    }
}
