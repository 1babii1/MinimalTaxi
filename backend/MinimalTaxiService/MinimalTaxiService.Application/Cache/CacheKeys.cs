using MinimalTaxiService.Domain.Enums;

namespace MinimalTaxiService.Application.Cache;

public static class CacheKeys
{
    public static string Profile(Guid userId) => $"profile:{userId}";

    public static string UserTrips(Guid userId, bool onlyActive) => $"user-trips:{userId}:{onlyActive}";

    public static string NearbyTrips(double latitude, double longitude, int radiusMeters, string? city, TripType? tripType, int limit)
        => $"nearby:{latitude:F6}:{longitude:F6}:{radiusMeters}:{city ?? "all"}:{tripType?.ToString() ?? "all"}:{limit}";
}
