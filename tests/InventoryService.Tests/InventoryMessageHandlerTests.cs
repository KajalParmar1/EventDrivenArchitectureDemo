using System.Text.Json;
using EdaDemo.Contracts;
using EdaDemo.Messaging;
using InventoryService;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InventoryService.Tests;

public class InventoryMessageHandlerTests
{
    [Fact]
    public async Task Publishes_InventoryReserved_when_stock_can_be_held()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var handler = CreateHandler(publisher);
        var order = CreateOrder(2);

        await handler.HandleAsync(Wrap(order), CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            "inventory-events",
            order.OrderId.ToString(),
            EventTypes.InventoryReserved,
            Arg.Any<Guid>(),
            order.CorrelationId,
            Arg.Any<DateTime>(),
            Arg.Is<InventoryReserved>(r => r.Success && r.OrderId == order.OrderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_publish_when_reservation_fails()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var handler = CreateHandler(publisher);
        var order = CreateOrder(5000);

        await Assert.ThrowsAsync<InventoryReservationException>(
            () => handler.HandleAsync(Wrap(order), CancellationToken.None));

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync<InventoryReserved>(
            default!, default!, default!, default, default, default, default!, default);
    }

    private static InventoryMessageHandler CreateHandler(IEventPublisher publisher)
    {
        var kafka = Options.Create(new KafkaOptions
        {
            ConsumerGroup = "inventory-service",
            Topics = new KafkaTopics { OrderEvents = "order-events", InventoryEvents = "inventory-events" }
        });
        var inventory = Options.Create(new InventoryOptions { FailWhenTotalQuantityExceeds = 1000 });
        return new InventoryMessageHandler(publisher, kafka, inventory, NullLogger<InventoryMessageHandler>.Instance);
    }

    private static OrderCreated CreateOrder(int quantity) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "customer-001",
            quantity * 10m,
            DateTime.UtcNow,
            [new OrderItem("product-001", quantity, 10m)]);

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
