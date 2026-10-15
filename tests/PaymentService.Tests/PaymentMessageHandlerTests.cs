using System.Text.Json;
using EdaDemo.Contracts;
using EdaDemo.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PaymentService;

namespace PaymentService.Tests;

public class PaymentMessageHandlerTests
{
    [Fact]
    public async Task Publishes_PaymentProcessed_for_a_successful_order()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var handler = CreateHandler(publisher, failForOrdersAbove: 10000m);
        var order = CreateOrder(50m);
        var message = Wrap(order);

        await handler.HandleAsync(message, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            "payment-events",
            order.OrderId.ToString(),
            EventTypes.PaymentProcessed,
            Arg.Any<Guid>(),
            order.CorrelationId,
            Arg.Any<DateTime>(),
            Arg.Is<PaymentProcessed>(p => p.Success && p.OrderId == order.OrderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_publish_when_payment_fails()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var handler = CreateHandler(publisher, failForOrdersAbove: 10000m);
        var order = CreateOrder(25000m);

        await Assert.ThrowsAsync<PaymentProcessingException>(
            () => handler.HandleAsync(Wrap(order), CancellationToken.None));

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync<PaymentProcessed>(
            default!, default!, default!, default, default, default, default!, default);
    }

    [Fact]
    public async Task Ignores_non_order_created_events()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var handler = CreateHandler(publisher, 10000m);
        var payment = new PaymentProcessed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, DateTime.UtcNow);
        var envelope = EventEnvelope<PaymentProcessed>.Create(
            payment, EventTypes.PaymentProcessed, payment.EventId, payment.CorrelationId, payment.OccurredAt);
        var json = JsonSerializer.Serialize(envelope, KafkaJson.Options);
        var consumed = new ConsumedEvent
        {
            Envelope = JsonSerializer.Deserialize<EventEnvelope>(json, KafkaJson.Options)!,
            Topic = "payment-events",
            RawValue = json,
            Key = payment.OrderId.ToString()
        };

        await handler.HandleAsync(consumed, CancellationToken.None);

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync<PaymentProcessed>(
            default!, default!, default!, default, default, default, default!, default);
    }

    private static PaymentMessageHandler CreateHandler(IEventPublisher publisher, decimal failForOrdersAbove)
    {
        var kafka = Options.Create(new KafkaOptions
        {
            ConsumerGroup = "payment-service",
            Topics = new KafkaTopics { OrderEvents = "order-events", PaymentEvents = "payment-events" }
        });
        var payment = Options.Create(new PaymentOptions { FailForOrdersAbove = failForOrdersAbove });
        return new PaymentMessageHandler(publisher, kafka, payment, NullLogger<PaymentMessageHandler>.Instance);
    }

    private static OrderCreated CreateOrder(decimal total) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "customer-001",
            total,
            DateTime.UtcNow,
            [new OrderItem("product-001", 1, total)]);

    private static ConsumedEvent Wrap(OrderCreated order)
    {
        var envelope = EventEnvelope<OrderCreated>.Create(
            order, EventTypes.OrderCreated, order.EventId, order.CorrelationId, order.OccurredAt);
        var json = JsonSerializer.Serialize(envelope, KafkaJson.Options);
        return new ConsumedEvent
        {
            Envelope = JsonSerializer.Deserialize<EventEnvelope>(json, KafkaJson.Options)!,
            Topic = "order-events",
            RawValue = json,
            Key = order.OrderId.ToString()
        };
    }
}
