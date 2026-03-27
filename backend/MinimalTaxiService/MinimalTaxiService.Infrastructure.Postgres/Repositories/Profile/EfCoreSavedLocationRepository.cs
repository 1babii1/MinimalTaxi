using Microsoft.EntityFrameworkCore;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Infrastructure.Postgres.Repositories.Profile;

public class EfCoreSavedLocationRepository : ISavedLocationRepository
{
    private readonly MinimalTaxiDbContext _dbContext;

    public EfCoreSavedLocationRepository(MinimalTaxiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(SavedLocation savedLocation, CancellationToken cancellationToken)
    {
        await _dbContext.SavedLocations.AddAsync(savedLocation, cancellationToken);
    }

    public async Task<SavedLocation?> GetByIdWithLock(Guid locationId, CancellationToken cancellationToken)
    {
        return await _dbContext.SavedLocations
            .FromSqlInterpolated($"SELECT * FROM saved_locations WHERE id = {locationId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SavedLocation>> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.SavedLocations
            .AsNoTracking()
            .Where(savedLocation => savedLocation.UserId == userId)
            .OrderByDescending(savedLocation => savedLocation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Remove(SavedLocation savedLocation)
    {
        _dbContext.SavedLocations.Remove(savedLocation);
    }
}
