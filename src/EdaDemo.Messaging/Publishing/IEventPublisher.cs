namespace EdaDemo.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TPayload>(
        string topic,
        string messageKey,
        string eventType,
        Guid eventId,
        Guid correlationId,
        DateTime occurredAt,
        TPayload payload,
        CancellationToken cancellationToken = default);

    Task PublishRawAsync(
        string topic,
        string messageKey,
        string json,
        CancellationToken cancellationToken = default);
}
