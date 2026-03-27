using MinimalTaxiService.Contracts.Profiles;

namespace MinimalTaxiService.Application.Database;

public interface IProfileReadRepository
{
    Task<ProfileDto?> GetProfile(Guid userId, CancellationToken cancellationToken);
}
