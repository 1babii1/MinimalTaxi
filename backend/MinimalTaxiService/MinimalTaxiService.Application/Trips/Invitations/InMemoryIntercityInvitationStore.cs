using System.Collections.Concurrent;

namespace MinimalTaxiService.Application.Trips.Invitations;

public sealed class InMemoryIntercityInvitationStore : IIntercityInvitationStore
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<Guid, IntercityInvitation> _store = new();

    public IntercityInvitation? GetById(Guid invitationId)
    {
        ExpireStale();
        _store.TryGetValue(invitationId, out var invitation);
        return invitation;
    }

    public IReadOnlyList<IntercityInvitation> GetForPassenger(Guid passengerId)
    {
        ExpireStale();
        return _store.Values
            .Where(invitation => invitation.PassengerId == passengerId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToList();
    }

    public IReadOnlyList<IntercityInvitation> GetForPassengerTrip(Guid passengerTripId)
    {
        ExpireStale();
        return _store.Values
            .Where(invitation => invitation.PassengerTripId == passengerTripId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToList();
    }

    public IntercityInvitation Add(Guid passengerTripId, Guid driverTripId, Guid passengerId, Guid driverId)
    {
        ExpireStale();

        var now = DateTimeOffset.UtcNow;
        var invitation = new IntercityInvitation
        {
            Id = Guid.NewGuid(),
            PassengerTripId = passengerTripId,
            DriverTripId = driverTripId,
            PassengerId = passengerId,
            DriverId = driverId,
            CreatedAt = now,
            ExpiresAt = now.Add(InviteLifetime),
            Status = IntercityInvitationStatus.Pending,
        };

        _store[invitation.Id] = invitation;
        return invitation;
    }

    public void MarkAccepted(Guid invitationId)
    {
        if (!_store.TryGetValue(invitationId, out var invitation))
            return;

        invitation.Status = IntercityInvitationStatus.Accepted;
        invitation.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var other in _store.Values.Where(item =>
                     item.Id != invitationId &&
                     item.PassengerTripId == invitation.PassengerTripId &&
                     item.Status == IntercityInvitationStatus.Pending))
        {
            other.Status = IntercityInvitationStatus.Expired;
            other.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void MarkDeclined(Guid invitationId)
    {
        if (!_store.TryGetValue(invitationId, out var invitation))
            return;

        invitation.Status = IntercityInvitationStatus.Declined;
        invitation.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExpired(Guid invitationId)
    {
        if (!_store.TryGetValue(invitationId, out var invitation))
            return;

        invitation.Status = IntercityInvitationStatus.Expired;
        invitation.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ExpireStale()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var invitation in _store.Values.Where(item =>
                     item.Status == IntercityInvitationStatus.Pending && item.ExpiresAt <= now))
        {
            invitation.Status = IntercityInvitationStatus.Expired;
            invitation.UpdatedAt = now;
        }
    }
}
