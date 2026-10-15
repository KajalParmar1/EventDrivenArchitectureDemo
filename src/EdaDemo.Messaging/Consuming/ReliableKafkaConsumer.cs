using System.Text.Json;
using Confluent.Kafka;
using EdaDemo.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdaDemo.Messaging;

public sealed class ReliableKafkaConsumer : BackgroundService
{
    private readonly IKafkaMessageHandler _handler;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDeadLetterPublisher _deadLetterPublisher;
    private readonly ExponentialBackoffRetryExecutor _retryExecutor;
    private readonly KafkaOptions _options;
    private readonly ILogger<ReliableKafkaConsumer> _logger;

    public ReliableKafkaConsumer(
        IKafkaMessageHandler handler,
        IServiceScopeFactory scopeFactory,
        IDeadLetterPublisher deadLetterPublisher,
        ExponentialBackoffRetryExecutor retryExecutor,
        IOptions<KafkaOptions> options,
        ILogger<ReliableKafkaConsumer> logger)
    {
        _handler = handler;
        _scopeFactory = scopeFactory;
        _deadLetterPublisher = deadLetterPublisher;
        _retryExecutor = retryExecutor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _handler.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(_handler.Topics);
        _logger.LogInformation(
            "Kafka consumer started GroupId={GroupId} ConsumerName={ConsumerName} Topics={Topics}",
            _handler.GroupId,
            _handler.ConsumerName,
            string.Join(",", _handler.Topics));

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
                    _logger.LogError(ex, "Failed to consume Kafka message.");
                    continue;
                }

                if (result?.Message?.Value is null)
                {
                    continue;
                }

                await ProcessAsync(consumer, result, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        EventEnvelope? envelope = null;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Message.Value, KafkaJson.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON on topic {Topic}. Moving message to DLQ.", result.Topic);
            await _deadLetterPublisher.PublishAsync(
                result.Topic,
                result.Message.Key,
                _handler.ConsumerName,
                result.Message.Value,
                ex,
                cancellationToken);
            consumer.Commit(result);
            return;
        }

        if (envelope is null)
        {
            _logger.LogError("Null envelope on topic {Topic}. Moving message to DLQ.", result.Topic);
            await _deadLetterPublisher.PublishAsync(
                result.Topic,
                result.Message.Key,
                _handler.ConsumerName,
                result.Message.Value,
                new InvalidOperationException("Envelope deserialized to null."),
                cancellationToken);
            consumer.Commit(result);
            return;
        }

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = TryGetOrderId(envelope),
            ["CorrelationId"] = envelope.CorrelationId,
            ["EventId"] = envelope.EventId,
            ["EventType"] = envelope.EventType,
            ["Service"] = _handler.ConsumerName
        });

        await using var scope = _scopeFactory.CreateAsyncScope();
        var idempotencyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

        var alreadyProcessed = await idempotencyStore.HasBeenProcessedAsync(
            envelope.EventId,
            _handler.ConsumerName,
            cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Skipping already processed event EventId={EventId} ConsumerName={ConsumerName}",
                envelope.EventId,
                _handler.ConsumerName);
            consumer.Commit(result);
            return;
        }

        var consumed = new ConsumedEvent
        {
            Envelope = envelope,
            Topic = result.Topic,
            RawValue = result.Message.Value,
            Key = result.Message.Key
        };

        try
        {
            await _retryExecutor.ExecuteAsync(
                ct => _handler.HandleAsync(consumed, ct),
                _options.RetryCount,
                _options.RetryDelaySeconds,
                _logger,
                cancellationToken);

            await idempotencyStore.MarkProcessedAsync(
                envelope.EventId,
                _handler.ConsumerName,
                cancellationToken);

            consumer.Commit(result);
        }
        catch (RetryExhaustedException ex)
        {
            await _deadLetterPublisher.PublishAsync(
                result.Topic,
                result.Message.Key,
                _handler.ConsumerName,
                result.Message.Value,
                ex.InnerException ?? ex,
                cancellationToken);

            consumer.Commit(result);
        }
    }

    private static object TryGetOrderId(EventEnvelope envelope)
    {
        if (envelope.Payload.ValueKind == JsonValueKind.Object &&
            envelope.Payload.TryGetProperty("orderId", out var orderId))
        {
            return orderId.ToString();
        }

        return string.Empty;
    }
}
