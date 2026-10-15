using EdaDemo.Contracts;

namespace EdaDemo.Messaging;

public sealed class ConsumedEvent
{
    public required EventEnvelope Envelope { get; init; }
    public required string Topic { get; init; }
    public required string RawValue { get; init; }
    public string? Key { get; init; }
}

public interface IKafkaMessageHandler
{
    IReadOnlyCollection<string> Topics { get; }
    string GroupId { get; }
    string ConsumerName { get; }
    Task HandleAsync(ConsumedEvent message, CancellationToken cancellationToken);
}
