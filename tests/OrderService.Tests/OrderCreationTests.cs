using System.Text.Json;
using EdaDemo.Contracts;
using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OrderService.Application;
using OrderService.Data;
using OrderService.Dtos;

namespace OrderService.Tests;

public class OrderCreationTests
{
    [Fact]
    public async Task Create_order_persists_order_items_and_outbox_in_one_save()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreateOrderAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal("Created", result.Status);
        Assert.NotEqual(Guid.Empty, result.OrderId);

        var order = await db.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal(result.OrderId, order.Id);
        Assert.Equal("customer-001", order.CustomerId);
        Assert.Equal(100m, order.TotalAmount);
        Assert.Single(order.Items);
        Assert.Equal("product-001", order.Items[0].ProductId);

        var outbox = await db.OutboxMessages.SingleAsync();
        Assert.Equal(EventTypes.OrderCreated, outbox.EventType);
        Assert.Equal("order-events", outbox.Topic);
        Assert.Equal(order.Id.ToString(), outbox.MessageKey);
        Assert.Null(outbox.ProcessedAt);

        var envelope = JsonSerializer.Deserialize<EventEnvelope>(outbox.Payload, KafkaJson.Options);
        Assert.NotNull(envelope);
        Assert.Equal(EventTypes.OrderCreated, envelope.EventType);
        Assert.Equal(order.CorrelationId, envelope.CorrelationId);
        var payload = envelope.DeserializePayload<OrderCreated>(KafkaJson.Options);
        Assert.Equal(order.Id, payload.OrderId);
        Assert.Equal(100m, payload.TotalAmount);
    }

    [Fact]
    public async Task Get_order_returns_the_created_order()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.CreateOrderAsync(ValidRequest(), CancellationToken.None);

        var loaded = await service.GetOrderAsync(created.OrderId, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(created.OrderId, loaded.OrderId);
        Assert.Equal("Created", loaded.Status);
        Assert.Single(loaded.Items);
    }

    [Fact]
    public async Task Get_order_returns_null_when_missing()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var loaded = await service.GetOrderAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(loaded);
    }

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("customer-001", 0, 10)]
    [InlineData("customer-001", 1, -1)]
    public void Validation_rejects_invalid_requests(string customerId, int quantity, decimal unitPrice)
    {
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = [new CreateOrderItemRequest { ProductId = "product-001", Quantity = quantity, UnitPrice = unitPrice }]
        };

        Assert.Throws<ArgumentException>(() => OrderApplicationService.Validate(request));
    }

    [Fact]
    public void Validation_rejects_empty_items()
    {
        var request = new CreateOrderRequest { CustomerId = "customer-001", Items = [] };
        Assert.Throws<ArgumentException>(() => OrderApplicationService.Validate(request));
    }

    private static CreateOrderRequest ValidRequest() => new()
    {
        CustomerId = "customer-001",
        Items =
        [
            new CreateOrderItemRequest
            {
                ProductId = "product-001",
                Quantity = 2,
                UnitPrice = 50m
            }
        ]
    };

    private static OrderApplicationService CreateService(OrderDbContext db)
    {
        var kafka = Options.Create(new KafkaOptions
        {
            Topics = new KafkaTopics { OrderEvents = "order-events" }
        });
        return new OrderApplicationService(db, kafka, NullLogger<OrderApplicationService>.Instance);
    }

    private static OrderDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }
}
