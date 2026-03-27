using MinimalTaxiService.Application.Database;
using Dapper;
using MinimalTaxiService.Contracts.Trips;
using MinimalTaxiService.Domain.Enums;

namespace MinimalTaxiService.Infrastructure.Postgres.Repositories.Trip;

public class NpgsqlTripReadRepository : ITripReadRepository
{
	private readonly IDbConnectionFactory _connectionFactory;

	public NpgsqlTripReadRepository(IDbConnectionFactory connectionFactory)
	{
		_connectionFactory = connectionFactory;
	}

	public async Task<IReadOnlyList<NearbyTripDto>> GetNearbyTrips(
		double latitude,
		double longitude,
		int radiusMeters,
		string? city,
		string? fromAddress,
		string? toAddress,
		double? fromLatitude,
		double? fromLongitude,
		int? fromRadiusMeters,
		double? toLatitude,
		double? toLongitude,
		int? toRadiusMeters,
		TripType? tripType,
		int limit,
		int offset,
		bool includeInactive,
		CancellationToken cancellationToken)
	{
		const string sql = @"
						   SELECT
							   t.id AS TripId,
							   t.type AS TripType,
							   t.status AS Status,
							   t.passenger_id AS PassengerId,
							   t.driver_id AS DriverId,
							   CASE
								   WHEN t.type = 'Intercity' THEN t.passenger_id <> '00000000-0000-0000-0000-000000000000'::uuid
								   ELSE FALSE
							   END AS IsPassengerRequest,
							   ST_Y(t.origin_location::geometry) AS Latitude,
							   ST_X(t.origin_location::geometry) AS Longitude,
							   CASE WHEN t.destination_location IS NULL THEN NULL ELSE ST_Y(t.destination_location::geometry) END AS DestinationLatitude,
							   CASE WHEN t.destination_location IS NULL THEN NULL ELSE ST_X(t.destination_location::geometry) END AS DestinationLongitude,
							   t.city AS City,
							   p.display_name AS PassengerName,
							   p.phone_number AS PassengerPhone,
							   COALESCE(d.from_address, t.from_address) AS FromAddress,
							   COALESCE(d.to_address, t.to_address) AS ToAddress,
							   d.departure_at AS DepartureAt,
							   u.display_name AS DriverName,
							   u.phone_number AS DriverPhone,
							   u.car_brand AS DriverCarBrand,
							   u.car_model AS DriverCarModel,
							   u.car_color AS DriverCarColor,
							   u.car_plate_number AS DriverCarPlateNumber,
							   d.total_seats AS TotalSeats,
							   d.available_seats AS AvailableSeats,
							   d.required_seats AS RequiredSeats,
							   CASE
								   WHEN @Latitude = 0 AND @Longitude = 0 THEN 0
								   ELSE ST_Distance(
									   t.origin_location,
									   ST_SetSRID(ST_MakePoint(@Longitude, @Latitude), 4326)::geography
								   )
							   END AS DistanceMeters,
							   t.created_at AS CreatedAt
						   FROM trips t
						   LEFT JOIN intercity_trip_details d ON d.trip_id = t.id
						   LEFT JOIN users u ON u.id = t.driver_id
						   LEFT JOIN users p ON p.id = t.passenger_id
						   WHERE (@IncludeInactive = TRUE OR t.status IN ('Created', 'DriverAccepted'))
							 AND (
								 (@Latitude = 0 AND @Longitude = 0)
								 OR ST_DWithin(
									 t.origin_location,
									 ST_SetSRID(ST_MakePoint(@Longitude, @Latitude), 4326)::geography,
									 @RadiusMeters
								 )
							 )
							 AND (@City IS NULL OR t.city = @City)
							 AND (@FromAddress IS NULL OR COALESCE(d.from_address, t.from_address) ILIKE CONCAT('%', @FromAddress, '%'))
							 AND (@ToAddress IS NULL OR COALESCE(d.to_address, t.to_address) ILIKE CONCAT('%', @ToAddress, '%'))
							 AND (
								 @FromLatitude IS NULL
								 OR @FromLongitude IS NULL
								 OR @FromRadiusMeters IS NULL
								 OR ST_DWithin(
									 t.origin_location,
									 ST_SetSRID(ST_MakePoint(@FromLongitude, @FromLatitude), 4326)::geography,
									 @FromRadiusMeters
								 )
							 )
							 AND (
								 @ToLatitude IS NULL
								 OR @ToLongitude IS NULL
								 OR @ToRadiusMeters IS NULL
								 OR (
									 t.destination_location IS NOT NULL
									 AND ST_DWithin(
										 t.destination_location,
										 ST_SetSRID(ST_MakePoint(@ToLongitude, @ToLatitude), 4326)::geography,
										 @ToRadiusMeters
									 )
								 )
							 )
							 AND (@TripType IS NULL OR t.type = @TripType)
						   ORDER BY
							   CASE WHEN t.type = 'Intercity' THEN COALESCE(d.departure_at, t.created_at) END ASC NULLS LAST,
							   t.created_at DESC
						   LIMIT @Limit
						   OFFSET @Offset
						   ";

		using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
		var result = await connection.QueryAsync<NearbyTripDto>(sql, new
		{
			Latitude = latitude,
			Longitude = longitude,
			RadiusMeters = radiusMeters,
			City = city,
			FromAddress = fromAddress,
			ToAddress = toAddress,
			FromLatitude = fromLatitude,
			FromLongitude = fromLongitude,
			FromRadiusMeters = fromRadiusMeters,
			ToLatitude = toLatitude,
			ToLongitude = toLongitude,
			ToRadiusMeters = toRadiusMeters,
			TripType = tripType?.ToString(),
			Limit = limit,
			Offset = offset,
			IncludeInactive = includeInactive
		});

		return result.ToList();
	}

	public async Task<IReadOnlyList<IntercityPassengerDto>> GetIntercityPassengersForDriver(
		Guid tripId,
		Guid driverId,
		CancellationToken cancellationToken)
	{
		const string sql = @"
						   SELECT
							   tp.user_id AS UserId,
							   u.display_name AS Name,
							   u.phone_number AS Phone,
							   tp.booked_seats AS Seats,
							   tp.pickup_address AS PickupAddress,
							   tp.dropoff_address AS DropoffAddress,
							   CASE
								   WHEN tp.pickup_location IS NULL THEN NULL
								   ELSE ST_Distance(t.origin_location, tp.pickup_location)
							   END AS DistanceMetersFromOrigin
						   FROM trip_participants tp
						   INNER JOIN trips t ON t.id = tp.trip_id
						   INNER JOIN users u ON u.id = tp.user_id
						   WHERE tp.trip_id = @TripId
							 AND t.driver_id = @DriverId
							 AND tp.is_driver = FALSE
						   ORDER BY tp.booked_seats DESC, u.display_name ASC
						   ";

		using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
		var result = await connection.QueryAsync<IntercityPassengerDto>(sql, new
		{
			TripId = tripId,
			DriverId = driverId,
		});

		return result.ToList();
	}
}