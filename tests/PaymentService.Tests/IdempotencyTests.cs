using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;

namespace PaymentService.Tests;

public class IdempotencyTests
{
    [Fact]
    public async Task Same_event_is_not_processed_twice_for_the_same_consumer()
    {
        await using var db = CreateDb();
        var store = new EfIdempotencyStore<PaymentDbContext>(db);
        var eventId = Guid.NewGuid();

        Assert.False(await store.HasBeenProcessedAsync(eventId, "payment-service"));

        await store.MarkProcessedAsync(eventId, "payment-service");

        Assert.True(await store.HasBeenProcessedAsync(eventId, "payment-service"));
        Assert.False(await store.HasBeenProcessedAsync(eventId, "inventory-service"));
    }

    [Fact]
    public async Task Marking_a_duplicate_event_is_a_no_op()
    {
        await using var db = CreateDb();
        var store = new EfIdempotencyStore<PaymentDbContext>(db);
        var eventId = Guid.NewGuid();

        await store.MarkProcessedAsync(eventId, "payment-service");
        await store.MarkProcessedAsync(eventId, "payment-service");

        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    private static PaymentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentDbContext(options);
    }
}
