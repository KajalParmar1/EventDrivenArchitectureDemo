using EdaDemo.Contracts;
using EdaDemo.Messaging;
using Microsoft.Extensions.Options;

namespace InventoryService;

public sealed class InventoryMessageHandler : IKafkaMessageHandler
{
    private readonly IEventPublisher _publisher;
    private readonly KafkaOptions _kafka;
    private readonly InventoryProcessor _processor;
    private readonly ILogger<InventoryMessageHandler> _logger;

    public InventoryMessageHandler(
        IEventPublisher publisher,
        IOptions<KafkaOptions> kafka,
        IOptions<InventoryOptions> inventory,
        ILogger<InventoryMessageHandler> logger)
    {
        _publisher = publisher;
        _kafka = kafka.Value;
        _processor = new InventoryProcessor(inventory.Value.FailWhenTotalQuantityExceeds);
        _logger = logger;
    }

    public IReadOnlyCollection<string> Topics => [_kafka.Topics.OrderEvents];
    public string GroupId => string.IsNullOrWhiteSpace(_kafka.ConsumerGroup) ? "inventory-service" : _kafka.ConsumerGroup;
    public string ConsumerName => "inventory-service";

    public async Task HandleAsync(ConsumedEvent message, CancellationToken cancellationToken)
    {
        if (message.Envelope.EventType != EventTypes.OrderCreated)
        {
            return;
        }

        var order = message.Envelope.DeserializePayload<OrderCreated>(KafkaJson.Options);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = order.OrderId,
            ["CorrelationId"] = order.CorrelationId,
            ["EventId"] = message.Envelope.EventId,
            ["EventType"] = EventTypes.InventoryReserved,
            ["Service"] = ConsumerName
        }))
        {
            _logger.LogInformation(
                "Inventory reservation started OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId}",
                order.OrderId,
                order.CorrelationId,
                message.Envelope.EventId);

            InventoryReserved reserved;
            try
            {
                reserved = _processor.Reserve(order);
            }
            catch (InventoryReservationException ex)
            {
                _logger.LogError(
                    ex,
                    "Inventory reservation failed OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId}",
                    order.OrderId,
                    order.CorrelationId,
                    message.Envelope.EventId);
                throw;
            }

            await _publisher.PublishAsync(
                _kafka.Topics.InventoryEvents,
                order.OrderId.ToString(),
                EventTypes.InventoryReserved,
                reserved.EventId,
                reserved.CorrelationId,
                reserved.OccurredAt,
                reserved,
                cancellationToken);

            _logger.LogInformation(
                "Inventory reserved OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId}",
                order.OrderId,
                order.CorrelationId,
                reserved.EventId);
        }
    }
}
