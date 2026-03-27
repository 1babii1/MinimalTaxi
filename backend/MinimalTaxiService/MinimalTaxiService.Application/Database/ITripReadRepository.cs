using MinimalTaxiService.Contracts.Trips;
using MinimalTaxiService.Domain.Enums;

namespace MinimalTaxiService.Application.Database;

public interface ITripReadRepository
{
    Task<IReadOnlyList<NearbyTripDto>> GetNearbyTrips(
        double latitude,
        double longitude,
        int radiusMeters,
        string? city,
        string? fromAddress,
        string? toAddress,
        double? fromLatitude,
        double? fromLongitude,
        int? fromRadiusMeters,
        double? toLatitude,
        double? toLongitude,
        int? toRadiusMeters,
        TripType? tripType,
        int limit,
        int offset,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IntercityPassengerDto>> GetIntercityPassengersForDriver(
        Guid tripId,
        Guid driverId,
        CancellationToken cancellationToken);
}
