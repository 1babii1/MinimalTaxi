using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Infrastructure.Postgres.Database;
using MinimalTaxiService.Infrastructure.Postgres.Repositories.Profile;
using MinimalTaxiService.Infrastructure.Postgres.Repositories.Trip;
using MinimalTaxiService.Infrastructure.Postgres.Repositories.User;

namespace MinimalTaxiService.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MinimalTaxiDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("MinimalTaxiServiceDb"),
                npgsql => npgsql.UseNetTopologySuite()));

        services.AddScoped<IReadDbContext>(provider => provider.GetRequiredService<MinimalTaxiDbContext>());

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<ITripRepository, EfCoreTripRepository>();
        services.AddScoped<IUserRepository, EfCoreUserRepository>();

        services.AddScoped<ITripReadRepository, NpgsqlTripReadRepository>();
        services.AddScoped<IUserTripsReadRepository, NpgsqlUserTripsReadRepository>();
        services.AddScoped<IProfileReadRepository, NpgsqlProfileReadRepository>();
        services.AddScoped<ISavedLocationRepository, EfCoreSavedLocationRepository>();

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }
}
