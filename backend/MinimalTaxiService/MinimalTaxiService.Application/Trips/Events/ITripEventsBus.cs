namespace MinimalTaxiService.Application.Trips.Events;

public interface ITripEventsBus
{
    IAsyncEnumerable<TripChangedEvent> Subscribe(CancellationToken cancellationToken);

    ValueTask PublishAsync(TripChangedEvent tripEvent, CancellationToken cancellationToken = default);
}
