using MinimalTaxiService.Application.Database;
using Dapper;
using MinimalTaxiService.Contracts.Trips;

namespace MinimalTaxiService.Infrastructure.Postgres.Repositories.User;

public class NpgsqlUserTripsReadRepository : IUserTripsReadRepository
{
	private readonly IDbConnectionFactory _connectionFactory;

	public NpgsqlUserTripsReadRepository(IDbConnectionFactory connectionFactory)
	{
		_connectionFactory = connectionFactory;
	}

	public async Task<IReadOnlyList<UserTripDto>> GetUserTrips(Guid userId, bool onlyActive, CancellationToken cancellationToken)
	{
		const string sql = @"
						   SELECT
							   t.id AS TripId,
							   t.type AS TripType,
							   t.status AS Status,
							   CASE
								   WHEN t.passenger_id = @UserId THEN TRUE
								   WHEN t.driver_id = @UserId AND (
									   t.passenger_id = @UserId
									   OR t.passenger_id = '00000000-0000-0000-0000-000000000000'::uuid
								   ) THEN TRUE
								   ELSE FALSE
							   END AS CreatedByUser,
							   t.passenger_id AS PassengerId,
							   t.city AS City,
							   COALESCE(d.from_address, t.from_address) AS FromAddress,
							   COALESCE(d.to_address, t.to_address) AS ToAddress,
							   ST_Y(t.origin_location::geometry) AS OriginLatitude,
							   ST_X(t.origin_location::geometry) AS OriginLongitude,
							   ST_Y(t.destination_location::geometry) AS DestinationLatitude,
							   ST_X(t.destination_location::geometry) AS DestinationLongitude,
							   t.driver_id AS DriverId,
							   p.display_name AS PassengerName,
							   p.phone_number AS PassengerPhone,
							   u.display_name AS DriverName,
							   u.phone_number AS DriverPhone,
							   u.car_brand AS DriverCarBrand,
							   u.car_model AS DriverCarModel,
							   u.car_color AS DriverCarColor,
							   u.car_plate_number AS DriverCarPlateNumber,
							   d.total_seats AS TotalSeats,
							   d.available_seats AS AvailableSeats,
							   d.required_seats AS RequiredSeats,
							   d.departure_at AS DepartureAt,
							   t.created_at AS CreatedAt
						   FROM trips t
						   LEFT JOIN intercity_trip_details d ON d.trip_id = t.id
						   LEFT JOIN users u ON u.id = t.driver_id
						   LEFT JOIN users p ON p.id = t.passenger_id
						   WHERE (t.passenger_id = @UserId OR t.driver_id = @UserId)
							 AND (
								 @OnlyActive = FALSE
								 OR t.status IN ('Created', 'DriverAccepted')
							 )
						   ORDER BY
							   CASE WHEN t.type = 'Intercity' THEN COALESCE(d.departure_at, t.created_at) END ASC NULLS LAST,
							   t.created_at DESC
						   ";

		using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
		var result = await connection.QueryAsync<UserTripDto>(sql, new { UserId = userId, OnlyActive = onlyActive });

		return result.ToList();
	}
}