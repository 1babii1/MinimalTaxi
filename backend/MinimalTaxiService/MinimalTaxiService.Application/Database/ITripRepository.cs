namespace MinimalTaxiService.Application.Database;

using MinimalTaxiService.Domain.ValueObjects;

public interface ITripRepository
{
	Task<int> GetActiveTripCountForPassenger(Guid passengerId, CancellationToken cancellationToken);

	Task<Domain.Entities.Trip?> GetTripByIdWithParticipants(Guid tripId, CancellationToken cancellationToken);

	Task<Domain.Entities.Trip?> GetTripByIdWithLock(Guid tripId, CancellationToken cancellationToken);

	Task<Domain.Entities.IntercityTripDetails?> GetIntercityDetailsByTripIdWithLock(Guid tripId, CancellationToken cancellationToken);

	Task AddTrip(Domain.Entities.Trip trip, CancellationToken cancellationToken);

	Task AddIntercityDetails(Domain.Entities.IntercityTripDetails details, CancellationToken cancellationToken);

	Task UpsertIntercityParticipant(
		Guid tripId,
		Guid userId,
		int seats,
		Location pickupLocation,
		Location dropoffLocation,
		string pickupAddress,
		string dropoffAddress,
		CancellationToken cancellationToken);

	Task ReopenPassengerRequestsLinkedToDriverOffer(
		Guid driverOfferTripId,
		Guid driverId,
		CancellationToken cancellationToken);
}