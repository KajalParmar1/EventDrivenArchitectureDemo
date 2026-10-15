namespace EdaDemo.Messaging;

public interface IIdempotencyStore
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);
}
