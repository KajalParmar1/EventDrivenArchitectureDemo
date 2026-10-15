using System.Text.Json;

namespace EdaDemo.Messaging;

public sealed class DeadLetterMessage
{
    public string OriginalTopic { get; init; } = string.Empty;
    public string? OriginalKey { get; init; }
    public string ConsumerName { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
    public JsonElement? OriginalPayload { get; init; }
}

public interface IDeadLetterPublisher
{
    Task PublishAsync(
        string originalTopic,
        string? originalKey,
        string consumerName,
        string originalPayloadJson,
        Exception exception,
        CancellationToken cancellationToken = default);
}

public sealed class KafkaDeadLetterPublisher : IDeadLetterPublisher
{
    private readonly IEventPublisher _publisher;
    private readonly KafkaOptions _options;

    public KafkaDeadLetterPublisher(IEventPublisher publisher, Microsoft.Extensions.Options.IOptions<KafkaOptions> options)
    {
        _publisher = publisher;
        _options = options.Value;
    }

    public Task PublishAsync(
        string originalTopic,
        string? originalKey,
        string consumerName,
        string originalPayloadJson,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        JsonElement? original = null;
        try
        {
            original = JsonSerializer.Deserialize<JsonElement>(originalPayloadJson, KafkaJson.Options);
        }
        catch (JsonException)
        {
            // Keep the raw string in Error context if the original payload was not valid JSON.
        }

        var message = new DeadLetterMessage
        {
            OriginalTopic = originalTopic,
            OriginalKey = originalKey,
            ConsumerName = consumerName,
            Error = exception.ToString(),
            FailedAt = DateTime.UtcNow,
            OriginalPayload = original
        };

        var json = JsonSerializer.Serialize(message, KafkaJson.Options);
        return _publisher.PublishRawAsync(
            _options.DeadLetterTopic(originalTopic),
            originalKey ?? string.Empty,
            json,
            cancellationToken);
    }
}
