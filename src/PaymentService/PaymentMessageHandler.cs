using EdaDemo.Contracts;
using EdaDemo.Messaging;
using Microsoft.Extensions.Options;

namespace PaymentService;

public sealed class PaymentMessageHandler : IKafkaMessageHandler
{
    private readonly IEventPublisher _publisher;
    private readonly KafkaOptions _kafka;
    private readonly PaymentProcessor _processor;
    private readonly ILogger<PaymentMessageHandler> _logger;

    public PaymentMessageHandler(
        IEventPublisher publisher,
        IOptions<KafkaOptions> kafka,
        IOptions<PaymentOptions> payment,
        ILogger<PaymentMessageHandler> logger)
    {
        _publisher = publisher;
        _kafka = kafka.Value;
        _processor = new PaymentProcessor(payment.Value.FailForOrdersAbove);
        _logger = logger;
    }

    public IReadOnlyCollection<string> Topics => [_kafka.Topics.OrderEvents];
    public string GroupId => string.IsNullOrWhiteSpace(_kafka.ConsumerGroup) ? "payment-service" : _kafka.ConsumerGroup;
    public string ConsumerName => "payment-service";

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
            ["EventType"] = EventTypes.PaymentProcessed,
            ["Service"] = ConsumerName
        }))
        {
            _logger.LogInformation(
                "Payment processing started OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId} TotalAmount={TotalAmount}",
                order.OrderId,
                order.CorrelationId,
                message.Envelope.EventId,
                order.TotalAmount);

            PaymentProcessed processed;
            try
            {
                processed = _processor.Process(order);
            }
            catch (PaymentProcessingException ex)
            {
                _logger.LogError(
                    ex,
                    "Payment failed OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId}",
                    order.OrderId,
                    order.CorrelationId,
                    message.Envelope.EventId);
                throw;
            }

            await _publisher.PublishAsync(
                _kafka.Topics.PaymentEvents,
                order.OrderId.ToString(),
                EventTypes.PaymentProcessed,
                processed.EventId,
                processed.CorrelationId,
                processed.OccurredAt,
                processed,
                cancellationToken);

            _logger.LogInformation(
                "Payment successful OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId}",
                order.OrderId,
                order.CorrelationId,
                processed.EventId);
        }
    }
}
