using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderService.Data;
using OrderService.Domain;
using OrderService.Infrastructure;

namespace OrderService.Tests;

public class OutboxProcessorTests
{
    [Fact]
    public async Task Publishes_unprocessed_outbox_messages_and_marks_them_processed()
    {
        await using var db = CreateDb();
        var publisher = Substitute.For<IEventPublisher>();
        var processor = new OutboxProcessor(db, publisher, NullLogger<OutboxProcessor>.Instance);

        var pending = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreated",
            Payload = """{"eventType":"OrderCreated"}""",
            OccurredAt = DateTime.UtcNow,
            Topic = "order-events",
            MessageKey = Guid.NewGuid().ToString()
        };
        var alreadyPublished = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreated",
            Payload = """{"eventType":"OrderCreated"}""",
            OccurredAt = DateTime.UtcNow.AddMinutes(-1),
            ProcessedAt = DateTime.UtcNow.AddMinutes(-1),
            Topic = "order-events",
            MessageKey = Guid.NewGuid().ToString()
        };
        db.OutboxMessages.AddRange(pending, alreadyPublished);
        await db.SaveChangesAsync();

        var published = await processor.PublishPendingAsync(20, CancellationToken.None);

        Assert.Equal(1, published);
        await publisher.Received(1).PublishRawAsync(
            pending.Topic,
            pending.MessageKey,
            pending.Payload,
            Arg.Any<CancellationToken>());
        Assert.NotNull((await db.OutboxMessages.FindAsync(pending.Id))!.ProcessedAt);
    }

    [Fact]
    public async Task Does_nothing_when_the_outbox_is_empty()
    {
        await using var db = CreateDb();
        var publisher = Substitute.For<IEventPublisher>();
        var processor = new OutboxProcessor(db, publisher, NullLogger<OutboxProcessor>.Instance);

        var published = await processor.PublishPendingAsync(20, CancellationToken.None);

        Assert.Equal(0, published);
        await publisher.DidNotReceiveWithAnyArgs().PublishRawAsync(default!, default!, default!, default);
    }

    private static OrderDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }
}
