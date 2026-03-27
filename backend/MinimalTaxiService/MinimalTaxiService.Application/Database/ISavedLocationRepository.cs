using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Application.Database;

public interface ISavedLocationRepository
{
    Task Add(SavedLocation savedLocation, CancellationToken cancellationToken);

    Task<SavedLocation?> GetByIdWithLock(Guid locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SavedLocation>> GetByUserId(Guid userId, CancellationToken cancellationToken);

    void Remove(SavedLocation savedLocation);
}