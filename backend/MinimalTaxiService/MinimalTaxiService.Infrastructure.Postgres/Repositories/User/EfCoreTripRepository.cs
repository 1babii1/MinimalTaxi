using MinimalTaxiService.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace MinimalTaxiService.Infrastructure.Postgres.Repositories.User;

public class EfCoreUserRepository : IUserRepository
{
	private readonly MinimalTaxiDbContext _dbContext;

	public EfCoreUserRepository(MinimalTaxiDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Domain.Entities.User?> GetById(Guid userId, CancellationToken cancellationToken)
	{
		return await _dbContext.Users
			.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
	}

	public async Task<Domain.Entities.User?> GetByIdWithLock(Guid userId, CancellationToken cancellationToken)
	{
		return await _dbContext.Users
			.FromSqlInterpolated($"SELECT * FROM users WHERE id = {userId} FOR UPDATE")
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task Add(Domain.Entities.User user, CancellationToken cancellationToken)
	{
		await _dbContext.Users.AddAsync(user, cancellationToken);
	}
}