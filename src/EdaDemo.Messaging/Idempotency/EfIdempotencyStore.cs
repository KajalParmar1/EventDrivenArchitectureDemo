using Microsoft.EntityFrameworkCore;

namespace EdaDemo.Messaging;

public sealed class EfIdempotencyStore<TContext> : IIdempotencyStore
    where TContext : DbContext
{
    private readonly TContext _db;

    public EfIdempotencyStore(TContext db)
    {
        _db = db;
    }

    public Task<bool> HasBeenProcessedAsync(
        Guid eventId,
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        return _db.Set<ProcessedEvent>().AnyAsync(
            e => e.EventId == eventId && e.ConsumerName == consumerName,
            cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid eventId,
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        var alreadyTracked = await HasBeenProcessedAsync(eventId, consumerName, cancellationToken);
        if (alreadyTracked)
        {
            return;
        }

        _db.Set<ProcessedEvent>().Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = consumerName,
            ProcessedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
