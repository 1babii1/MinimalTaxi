using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Infrastructure.Postgres;

public class MinimalTaxiDbContext : DbContext, IReadDbContext
{
    private readonly string _connectionString = null!;
    private ILoggerFactory? LoggerFactory { get; }

    public MinimalTaxiDbContext(string connectionString, ILoggerFactory? loggerFactory)
    {
        _connectionString = connectionString;
        LoggerFactory = loggerFactory;
    }

    public MinimalTaxiDbContext(DbContextOptions<MinimalTaxiDbContext> options, ILoggerFactory? loggerFactory)
        : base(options)
    {
        LoggerFactory = loggerFactory;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrWhiteSpace(_connectionString))
            optionsBuilder.UseNpgsql(_connectionString, npgsql => npgsql.UseNetTopologySuite());

        optionsBuilder.UseLoggerFactory(LoggerFactory);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MinimalTaxiDbContext).Assembly);
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<SavedLocation> SavedLocations => Set<SavedLocation>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<TripParticipant> TripParticipants => Set<TripParticipant>();
    public DbSet<IntercityTripDetails> IntercityTripDetails => Set<IntercityTripDetails>();

    public IQueryable<User> UserRead => Set<User>().AsNoTracking();
    public IQueryable<SavedLocation> SavedLocationRead => Set<SavedLocation>().AsNoTracking();
    public IQueryable<Trip> TripRead => Set<Trip>().AsNoTracking();
    public IQueryable<DriverLocation> DriverLocationRead => Set<DriverLocation>().AsNoTracking();
    public IQueryable<TripParticipant> TripParticipantRead => Set<TripParticipant>().AsNoTracking();
    public IQueryable<IntercityTripDetails> IntercityTripDetailsRead => Set<IntercityTripDetails>().AsNoTracking();

}