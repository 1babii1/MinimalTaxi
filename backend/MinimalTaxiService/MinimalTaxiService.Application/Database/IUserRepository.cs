namespace MinimalTaxiService.Application.Database;

public interface IUserRepository
{
	Task<Domain.Entities.User?> GetById(Guid userId, CancellationToken cancellationToken);

	Task<Domain.Entities.User?> GetByIdWithLock(Guid userId, CancellationToken cancellationToken);

	Task Add(Domain.Entities.User user, CancellationToken cancellationToken);
}