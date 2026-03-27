using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MinimalTaxiService.Application.Trips.Events;
using Microsoft.Extensions.Options;

namespace MinimalTaxiService.Web.Realtime.Kafka;

// Единая точка для trip events:
// 1) PublishAsync отправляет событие в Kafka.
// 2) Background consumer читает события из Kafka и фан-аутит их в SSE подписчиков.
public sealed class KafkaTripEventsBus : BackgroundService, ITripEventsBus
{
    private readonly ConcurrentDictionary<Guid, Channel<TripChangedEvent>> _subscribers = new();
    private readonly KafkaEventsOptions _options;
    private readonly ILogger<KafkaTripEventsBus> _logger;
    private readonly IProducer<string, string> _producer;

    public KafkaTripEventsBus(
        IOptions<KafkaEventsOptions> options,
        ILogger<KafkaTripEventsBus> logger)
    {
        _options = options.Value;
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 5000,
            EnableIdempotence = true,
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

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

    public async ValueTask PublishAsync(TripChangedEvent tripEvent, CancellationToken cancellationToken = default)
    {
        // В качестве ключа используем TripId, чтобы события одной поездки попадали в одну партицию и сохраняли порядок.
        var key = tripEvent.TripId.ToString("N");
        var payload = JsonSerializer.Serialize(tripEvent);

        await _producer.ProduceAsync(
            _options.Topic,
            new Message<string, string>
            {
                Key = key,
                Value = payload,
            },
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureTopicExists(stoppingToken);
        await Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task EnsureTopicExists(CancellationToken cancellationToken)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = _options.BootstrapServers,
        };

        using var admin = new AdminClientBuilder(adminConfig).Build();

        try
        {
            await admin.CreateTopicsAsync(
                new[]
                {
                    new TopicSpecification
                    {
                        Name = _options.Topic,
                        NumPartitions = 3,
                        ReplicationFactor = 1,
                    }
                });
        }
        catch (CreateTopicsException ex) when (
            ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // Топик уже есть — это нормальная ситуация.
        }
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true,
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka consumer error: {Reason}", error.Reason);
            })
            .Build();

        consumer.Subscribe(_options.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    // При первом старте топик может ещё не существовать.
                    // Не валим сервис, а пробуем снова через короткую паузу.
                    _logger.LogWarning(ex, "Kafka consume retry: {Reason}", ex.Error.Reason);
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    continue;
                }

                if (result?.Message?.Value is null)
                    continue;

                TripChangedEvent? tripEvent = null;
                try
                {
                    tripEvent = JsonSerializer.Deserialize<TripChangedEvent>(result.Message.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize trip event from Kafka");
                }

                if (tripEvent is null)
                    continue;

                foreach (var subscriber in _subscribers.Values)
                {
                    subscriber.Writer.TryWrite(tripEvent);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при остановке приложения.
        }
        finally
        {
            consumer.Close();
        }
    }

    public override void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
        base.Dispose();
    }
}
