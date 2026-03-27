namespace MinimalTaxiService.Application.Trips.Events;

public sealed record TripChangedEvent(
    Guid TripId,
    string EventType,
    DateTimeOffset OccurredAtUtc);
