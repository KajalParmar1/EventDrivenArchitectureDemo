using System.Text.Json;
using EdaDemo.Contracts;
using EdaDemo.Messaging;

namespace EdaDemo.Contracts.Tests;

public class EventContractTests
{
    [Fact]
    public void OrderCreated_serializes_inside_an_event_envelope()
    {
        var eventId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var payload = new OrderCreated(
            eventId,
            correlationId,
            orderId,
            "customer-001",
            100m,
            occurredAt,
            [new OrderItem("product-001", 2, 50m)]);

        var envelope = EventEnvelope<OrderCreated>.Create(
            payload,
            EventTypes.OrderCreated,
            eventId,
            correlationId,
            occurredAt);

        var json = JsonSerializer.Serialize(envelope, KafkaJson.Options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(eventId.ToString(), root.GetProperty("eventId").GetGuid().ToString());
        Assert.Equal("OrderCreated", root.GetProperty("eventType").GetString());
        Assert.Equal(correlationId.ToString(), root.GetProperty("correlationId").GetGuid().ToString());
        Assert.True(root.TryGetProperty("occurredAt", out _));
        Assert.Equal(orderId.ToString(), root.GetProperty("payload").GetProperty("orderId").GetGuid().ToString());
        Assert.Equal("customer-001", root.GetProperty("payload").GetProperty("customerId").GetString());
        Assert.Equal(100m, root.GetProperty("payload").GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public void Envelope_round_trips_to_non_generic_form()
    {
        var payload = new PaymentProcessed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, DateTime.UtcNow);
        var envelope = EventEnvelope<PaymentProcessed>.Create(
            payload,
            EventTypes.PaymentProcessed,
            payload.EventId,
            payload.CorrelationId,
            payload.OccurredAt);

        var json = JsonSerializer.Serialize(envelope, KafkaJson.Options);
        var deserialized = JsonSerializer.Deserialize<EventEnvelope>(json, KafkaJson.Options);

        Assert.NotNull(deserialized);
        Assert.Equal(EventTypes.PaymentProcessed, deserialized.EventType);
        var inner = deserialized.DeserializePayload<PaymentProcessed>(KafkaJson.Options);
        Assert.Equal(payload.OrderId, inner.OrderId);
        Assert.True(inner.Success);
    }

    [Fact]
    public void Event_type_names_are_stable()
    {
        Assert.Equal("OrderCreated", EventTypes.OrderCreated);
        Assert.Equal("PaymentProcessed", EventTypes.PaymentProcessed);
        Assert.Equal("InventoryReserved", EventTypes.InventoryReserved);
        Assert.Equal("NotificationSent", EventTypes.NotificationSent);
    }
}
