using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalTaxiService.Application.Database;
using Npgsql;

namespace MinimalTaxiService.Infrastructure.Postgres.Database;

public class NpgsqlConnectionFactory : IDisposable, IAsyncDisposable, IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILoggerFactory _loggerFactory;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());

        var connectionString = configuration.GetConnectionString("MinimalTaxiServiceDb")
                               ?? throw new InvalidOperationException("Connection string 'MinimalTaxiServiceDb' is not configured.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder
            .UseLoggerFactory(_loggerFactory);
        _dataSource = dataSourceBuilder.Build();
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    public void Dispose() => _dataSource.Dispose();

    public async ValueTask DisposeAsync() => await _dataSource.DisposeAsync();
}