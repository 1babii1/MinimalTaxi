using System.Globalization;
using System.Text.Json.Serialization;

namespace MinimalTaxiService.Web.Integrations.Yandex;

public sealed class YandexGeocodeResponse
{
    [JsonPropertyName("response")]
    public YandexResponseContainer? Response { get; set; }
}

public sealed class YandexResponseContainer
{
    [JsonPropertyName("GeoObjectCollection")]
    public YandexGeoObjectCollection? GeoObjectCollection { get; set; }
}

public sealed class YandexGeoObjectCollection
{
    [JsonPropertyName("featureMember")]
    public List<YandexFeatureMember>? FeatureMembers { get; set; }
}

public sealed class YandexFeatureMember
{
    [JsonPropertyName("GeoObject")]
    public YandexGeoObject? GeoObject { get; set; }
}

public sealed class YandexGeoObject
{
    [JsonPropertyName("Point")]
    public YandexPoint? Point { get; set; }

    [JsonPropertyName("metaDataProperty")]
    public YandexMetaDataProperty? MetaDataProperty { get; set; }
}

public sealed class YandexPoint
{
    [JsonPropertyName("pos")]
    public string? Pos { get; set; }
}

public sealed class YandexMetaDataProperty
{
    [JsonPropertyName("GeocoderMetaData")]
    public YandexGeocoderMetaData? GeocoderMetaData { get; set; }
}

public sealed class YandexGeocoderMetaData
{
    [JsonPropertyName("Address")]
    public YandexAddress? Address { get; set; }
}

public sealed class YandexAddress
{
    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }
}

public static class PosStringExtensions
{
    public static bool TryParseToLocation(this string? pos, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (string.IsNullOrWhiteSpace(pos))
            return false;

        var parts = pos.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        var lonOk = double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon);
        var latOk = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat);

        if (!lonOk || !latOk)
            return false;

        latitude = lat;
        longitude = lon;
        return true;
    }
}
