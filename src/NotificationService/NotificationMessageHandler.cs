using EdaDemo.Contracts;
using EdaDemo.Messaging;
using Microsoft.Extensions.Options;

namespace NotificationService;

public sealed class NotificationMessageHandler : IKafkaMessageHandler
{
    private readonly IEventPublisher _publisher;
    private readonly KafkaOptions _kafka;
    private readonly NotificationProcessor _processor;
    private readonly ILogger<NotificationMessageHandler> _logger;

    public NotificationMessageHandler(
        IEventPublisher publisher,
        IOptions<KafkaOptions> kafka,
        ILogger<NotificationMessageHandler> logger)
    {
        _publisher = publisher;
        _kafka = kafka.Value;
        _processor = new NotificationProcessor();
        _logger = logger;
    }

    public IReadOnlyCollection<string> Topics =>
    [
        _kafka.Topics.OrderEvents,
        _kafka.Topics.PaymentEvents,
        _kafka.Topics.InventoryEvents
    ];

    public string GroupId => string.IsNullOrWhiteSpace(_kafka.ConsumerGroup) ? "notification-service" : _kafka.ConsumerGroup;
    public string ConsumerName => "notification-service";

    public async Task HandleAsync(ConsumedEvent message, CancellationToken cancellationToken)
    {
        var (orderId, correlationId, description) = Describe(message);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = orderId,
            ["CorrelationId"] = correlationId,
            ["EventId"] = message.Envelope.EventId,
            ["EventType"] = EventTypes.NotificationSent,
            ["Service"] = ConsumerName
        }))
        {
            _logger.LogInformation(
                "Notification sent for Order {OrderId} ({Description}) CorrelationId={CorrelationId} EventId={EventId} SourceEventType={SourceEventType}",
                orderId,
                description,
                correlationId,
                message.Envelope.EventId,
                message.Envelope.EventType);

            var sent = _processor.CreateNotification(orderId, correlationId);

            await _publisher.PublishAsync(
                _kafka.Topics.NotificationEvents,
                orderId.ToString(),
                EventTypes.NotificationSent,
                sent.EventId,
                sent.CorrelationId,
                sent.OccurredAt,
                sent,
                cancellationToken);
        }
    }

    private static (Guid OrderId, Guid CorrelationId, string Description) Describe(ConsumedEvent message)
    {
        return message.Envelope.EventType switch
        {
            EventTypes.OrderCreated => DescribeOrderCreated(message),
            EventTypes.PaymentProcessed => DescribePayment(message),
            EventTypes.InventoryReserved => DescribeInventory(message),
            _ => (Guid.Empty, message.Envelope.CorrelationId, message.Envelope.EventType)
        };
    }

    private static (Guid, Guid, string) DescribeOrderCreated(ConsumedEvent message)
    {
        var payload = message.Envelope.DeserializePayload<OrderCreated>(KafkaJson.Options);
        return (payload.OrderId, payload.CorrelationId, "order created");
    }

    private static (Guid, Guid, string) DescribePayment(ConsumedEvent message)
    {
        var payload = message.Envelope.DeserializePayload<PaymentProcessed>(KafkaJson.Options);
        var result = payload.Success ? "payment succeeded" : "payment failed";
        return (payload.OrderId, payload.CorrelationId, result);
    }

    private static (Guid, Guid, string) DescribeInventory(ConsumedEvent message)
    {
        var payload = message.Envelope.DeserializePayload<InventoryReserved>(KafkaJson.Options);
        var result = payload.Success ? "inventory reserved" : "inventory reservation failed";
        return (payload.OrderId, payload.CorrelationId, result);
    }
}
