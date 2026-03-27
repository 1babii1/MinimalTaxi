namespace MinimalTaxiService.Application.Trips.Invitations;

public interface IIntercityInvitationStore
{
    IntercityInvitation? GetById(Guid invitationId);
    IReadOnlyList<IntercityInvitation> GetForPassenger(Guid passengerId);
    IReadOnlyList<IntercityInvitation> GetForPassengerTrip(Guid passengerTripId);
    IntercityInvitation Add(Guid passengerTripId, Guid driverTripId, Guid passengerId, Guid driverId);
    void MarkAccepted(Guid invitationId);
    void MarkDeclined(Guid invitationId);
    void MarkExpired(Guid invitationId);
    void ExpireStale();
}
