using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MinimalTaxiService.Application.Trips.Events;

namespace MinimalTaxiService.Web.Realtime;

public sealed class InMemoryTripEventsBus : ITripEventsBus
{
    private readonly ConcurrentDictionary<Guid, Channel<TripChangedEvent>> _subscribers = new();

    public async IAsyncEnumerable<TripChangedEvent> Subscribe([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<TripChangedEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        _subscribers.TryAdd(subscriptionId, channel);

        try
        {
            await foreach (var tripEvent in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return tripEvent;
            }
        }
        finally
        {
            _subscribers.TryRemove(subscriptionId, out _);
        }
    }

    public ValueTask PublishAsync(TripChangedEvent tripEvent, CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(tripEvent);
        }

        return ValueTask.CompletedTask;
    }
}
