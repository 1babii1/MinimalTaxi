using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MinimalTaxiService.Application.Database;
using Shared;

namespace MinimalTaxiService.Infrastructure.Postgres
{
    public class TransactionManager : ITransactionManager
    {
        private readonly MinimalTaxiDbContext _dbContext;
        private readonly ILogger<TransactionManager> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public TransactionManager(MinimalTaxiDbContext dbContext, ILogger<TransactionManager> logger,
            ILoggerFactory loggerFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public async Task<Result<ITransactionScope, Error>> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var transactionScopeLogger = _loggerFactory.CreateLogger<TransactionScope>();

                var transactionScope = new TransactionScope(transaction.GetDbTransaction(), transactionScopeLogger);

                return transactionScope;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin transaction");
                return Error.Failure("database", "Failed to begin transaction");
            }
        }

        public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return UnitResult.Success<Error>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes");
                return UnitResult.Failure<Error>(Error.Failure());
            }
        }
    }
}