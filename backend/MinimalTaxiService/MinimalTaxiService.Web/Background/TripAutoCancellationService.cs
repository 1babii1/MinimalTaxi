using Microsoft.EntityFrameworkCore;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Domain.Enums;
using MinimalTaxiService.Infrastructure.Postgres;

namespace MinimalTaxiService.Web.Background;

public sealed class TripAutoCancellationService : BackgroundService
{
    private static readonly TimeSpan LocalCreatedTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan IntercityAfterDepartureTtl = TimeSpan.FromHours(4);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TripAutoCancellationService> _logger;

    public TripAutoCancellationService(
        IServiceScopeFactory scopeFactory,
        ILogger<TripAutoCancellationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Trip auto-cancellation iteration failed");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MinimalTaxiDbContext>();
        var tripEventsBus = scope.ServiceProvider.GetRequiredService<ITripEventsBus>();

        var now = DateTimeOffset.UtcNow;
        var localCutoff = now - LocalCreatedTtl;
        var intercityCutoff = now - IntercityAfterDepartureTtl;

        var affectedTripIds = new List<Guid>();

        var localTripsToCancel = await dbContext.Trips
            .Where(trip => trip.Type == TripType.Local)
            .Where(trip => trip.Status == TripStatus.Created)
            .Where(trip => trip.DriverId == null)
            .Where(trip => trip.CreatedAt <= localCutoff)
            .ToListAsync(cancellationToken);

        foreach (var trip in localTripsToCancel)
        {
            var cancelResult = trip.CancelByPassenger();
            if (cancelResult.IsSuccess)
                affectedTripIds.Add(trip.Id);
        }

        var intercityTripsToCancel = await (
            from trip in dbContext.Trips
            join details in dbContext.IntercityTripDetails on trip.Id equals details.TripId
            where trip.Type == TripType.Intercity
            where trip.Status == TripStatus.Created
            where details.DepartureAt.HasValue && details.DepartureAt <= intercityCutoff
            select trip)
            .ToListAsync(cancellationToken);

        foreach (var trip in intercityTripsToCancel)
        {
            var cancelResult = trip.PassengerId == Guid.Empty
                ? trip.CancelByDriver()
                : trip.CancelByPassenger();

            if (cancelResult.IsSuccess)
                affectedTripIds.Add(trip.Id);
        }

        if (affectedTripIds.Count == 0)
            return;

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var tripId in affectedTripIds.Distinct())
        {
            await tripEventsBus.PublishAsync(
                new TripChangedEvent(tripId, "trip.auto_cancelled", DateTimeOffset.UtcNow),
                cancellationToken);
        }

        _logger.LogInformation("Auto-cancelled trips count: {Count}", affectedTripIds.Count);
    }
}
