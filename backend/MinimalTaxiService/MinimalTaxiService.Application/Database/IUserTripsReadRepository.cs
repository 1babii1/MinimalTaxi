using MinimalTaxiService.Contracts.Trips;

namespace MinimalTaxiService.Application.Database;

public interface IUserTripsReadRepository
{
    Task<IReadOnlyList<UserTripDto>> GetUserTrips(Guid userId, bool onlyActive, CancellationToken cancellationToken);
}
