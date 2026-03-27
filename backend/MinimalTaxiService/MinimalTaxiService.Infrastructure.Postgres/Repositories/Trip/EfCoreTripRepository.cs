using MinimalTaxiService.Application.Database;
using Microsoft.EntityFrameworkCore;
using MinimalTaxiService.Domain.Entities;
using MinimalTaxiService.Domain.ValueObjects;
using NetTopologySuite.Geometries;

namespace MinimalTaxiService.Infrastructure.Postgres.Repositories.Trip;

public class EfCoreTripRepository : ITripRepository
{
	private readonly MinimalTaxiDbContext _dbContext;

	public EfCoreTripRepository(MinimalTaxiDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<int> GetActiveTripCountForPassenger(Guid passengerId, CancellationToken cancellationToken)
	{
		return await _dbContext.Trips
			.Where(t => t.PassengerId == passengerId)
			.CountAsync(t => t.Status == Domain.Enums.TripStatus.Created || t.Status == Domain.Enums.TripStatus.DriverAccepted, cancellationToken);
	}

	public async Task<Domain.Entities.Trip?> GetTripByIdWithParticipants(Guid tripId, CancellationToken cancellationToken)
	{
		return await _dbContext.Trips
			.Include(t => t.Participants)
			.FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);
	}

	public async Task<Domain.Entities.Trip?> GetTripByIdWithLock(Guid tripId, CancellationToken cancellationToken)
	{
		return await _dbContext.Trips
			.FromSqlInterpolated($"SELECT * FROM trips WHERE id = {tripId} FOR UPDATE")
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<IntercityTripDetails?> GetIntercityDetailsByTripIdWithLock(Guid tripId, CancellationToken cancellationToken)
	{
		return await _dbContext.IntercityTripDetails
			.FromSqlInterpolated($"SELECT * FROM intercity_trip_details WHERE trip_id = {tripId} FOR UPDATE")
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task AddTrip(Domain.Entities.Trip trip, CancellationToken cancellationToken)
	{
		await _dbContext.Trips.AddAsync(trip, cancellationToken);
	}

	public async Task AddIntercityDetails(IntercityTripDetails details, CancellationToken cancellationToken)
	{
		await _dbContext.IntercityTripDetails.AddAsync(details, cancellationToken);
	}

	public async Task UpsertIntercityParticipant(
		Guid tripId,
		Guid userId,
		int seats,
		MinimalTaxiService.Domain.ValueObjects.Location pickupLocation,
		MinimalTaxiService.Domain.ValueObjects.Location dropoffLocation,
		string pickupAddress,
		string dropoffAddress,
		CancellationToken cancellationToken)
	{
		var participantId = Guid.NewGuid();
		var pickupPoint = new Point(pickupLocation.Longitude, pickupLocation.Latitude) { SRID = 4326 };
		var dropoffPoint = new Point(dropoffLocation.Longitude, dropoffLocation.Latitude) { SRID = 4326 };

		await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
			INSERT INTO trip_participants (
				id,
				trip_id,
				user_id,
				is_driver,
				booked_seats,
				pickup_location,
				dropoff_location,
				pickup_address,
				dropoff_address)
			VALUES (
				{participantId},
				{tripId},
				{userId},
				FALSE,
				{seats},
				{pickupPoint},
				{dropoffPoint},
				{pickupAddress},
				{dropoffAddress})
			ON CONFLICT (trip_id, user_id)
			DO UPDATE SET
				booked_seats = trip_participants.booked_seats + EXCLUDED.booked_seats,
				pickup_location = EXCLUDED.pickup_location,
				dropoff_location = EXCLUDED.dropoff_location,
				pickup_address = EXCLUDED.pickup_address,
				dropoff_address = EXCLUDED.dropoff_address
			WHERE trip_participants.is_driver = FALSE;
		", cancellationToken);
	}

	public async Task ReopenPassengerRequestsLinkedToDriverOffer(
		Guid driverOfferTripId,
		Guid driverId,
		CancellationToken cancellationToken)
	{
		await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
			UPDATE trips AS passenger_trips
			SET
				driver_id = NULL,
				status = 'Created',
				updated_at = NOW()
			WHERE passenger_trips.type = 'Intercity'
			  AND passenger_trips.status = 'DriverAccepted'
			  AND passenger_trips.driver_id = {driverId}
			  AND passenger_trips.passenger_id IN (
				SELECT participant.user_id
				FROM trip_participants AS participant
				WHERE participant.trip_id = {driverOfferTripId}
				  AND participant.is_driver = FALSE
			  );
		", cancellationToken);
	}
}