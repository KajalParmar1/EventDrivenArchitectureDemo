using System.Text.Json;
using Confluent.Kafka;
using EdaDemo.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdaDemo.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 5
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TPayload>(
        string topic,
        string messageKey,
        string eventType,
        Guid eventId,
        Guid correlationId,
        DateTime occurredAt,
        TPayload payload,
        CancellationToken cancellationToken = default)
    {
        var envelope = EventEnvelope<TPayload>.Create(payload, eventType, eventId, correlationId, occurredAt);
        var json = JsonSerializer.Serialize(envelope, KafkaJson.Options);
        await PublishRawAsync(topic, messageKey, json, cancellationToken);
    }

    public async Task PublishRawAsync(
        string topic,
        string messageKey,
        string json,
        CancellationToken cancellationToken = default)
    {
        var result = await _producer.ProduceAsync(
            topic,
            new Message<string, string> { Key = messageKey, Value = json },
            cancellationToken);

        _logger.LogInformation(
            "Published Kafka message Topic={Topic} Partition={Partition} Offset={Offset} Key={Key}",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            messageKey);
    }

    public void Dispose() => _producer.Dispose();
}
