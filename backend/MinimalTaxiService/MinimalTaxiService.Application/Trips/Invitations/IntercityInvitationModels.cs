namespace MinimalTaxiService.Application.Trips.Invitations;

public enum IntercityInvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
}

public sealed class IntercityInvitation
{
    public Guid Id { get; init; }
    public Guid PassengerTripId { get; init; }
    public Guid DriverTripId { get; init; }
    public Guid PassengerId { get; init; }
    public Guid DriverId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public IntercityInvitationStatus Status { get; set; }

    public bool IsActive =>
        Status == IntercityInvitationStatus.Pending &&
        ExpiresAt > DateTimeOffset.UtcNow;
}
