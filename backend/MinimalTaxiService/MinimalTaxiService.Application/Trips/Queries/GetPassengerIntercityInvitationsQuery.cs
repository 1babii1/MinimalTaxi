using MinimalTaxiService.Application.Trips.Invitations;

namespace MinimalTaxiService.Application.Trips.Queries;

public sealed record GetPassengerIntercityInvitationsQuery(Guid PassengerId);

public sealed class PassengerIntercityInvitationDto
{
    public Guid InvitationId { get; set; }
    public Guid PassengerTripId { get; set; }
    public Guid DriverTripId { get; set; }
    public Guid DriverId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class GetPassengerIntercityInvitationsQueryHandler(
    IIntercityInvitationStore invitationStore)
{
    public Task<IReadOnlyList<PassengerIntercityInvitationDto>> Handle(
        GetPassengerIntercityInvitationsQuery query,
        CancellationToken cancellationToken)
    {
        invitationStore.ExpireStale();

        IReadOnlyList<PassengerIntercityInvitationDto> result = invitationStore
            .GetForPassenger(query.PassengerId)
            .Where(invitation => invitation.Status == IntercityInvitationStatus.Pending)
            .Where(invitation => invitation.ExpiresAt > DateTimeOffset.UtcNow)
            .Select(invitation => new PassengerIntercityInvitationDto
            {
                InvitationId = invitation.Id,
                PassengerTripId = invitation.PassengerTripId,
                DriverTripId = invitation.DriverTripId,
                DriverId = invitation.DriverId,
                Status = invitation.Status.ToString(),
                CreatedAt = invitation.CreatedAt,
                ExpiresAt = invitation.ExpiresAt,
            })
            .ToList();

        return Task.FromResult(result);
    }
}
