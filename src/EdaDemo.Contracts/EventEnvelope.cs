using System.Text.Json;

namespace EdaDemo.Contracts;

public sealed class EventEnvelope
{
    public Guid EventId { get; init; }
    public required string EventType { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTime OccurredAt { get; init; }
    public JsonElement Payload { get; init; }

    public T DeserializePayload<T>(JsonSerializerOptions options)
    {
        return Payload.Deserialize<T>(options)
            ?? throw new InvalidOperationException($"Could not deserialize payload as {typeof(T).Name}.");
    }
}

public sealed class EventEnvelope<T>
{
    public Guid EventId { get; init; }
    public required string EventType { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTime OccurredAt { get; init; }
    public required T Payload { get; init; }

    public static EventEnvelope<T> Create(
        T payload,
        string eventType,
        Guid eventId,
        Guid correlationId,
        DateTime occurredAt)
    {
        return new EventEnvelope<T>
        {
            EventId = eventId,
            EventType = eventType,
            CorrelationId = correlationId,
            OccurredAt = occurredAt,
            Payload = payload
        };
    }
}
