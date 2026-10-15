using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;

namespace OrderService.Infrastructure;

public sealed class OutboxProcessor
{
    private readonly OrderDbContext _db;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        OrderDbContext db,
        IEventPublisher publisher,
        ILogger<OutboxProcessor> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<int> PublishPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pending = await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            await _publisher.PublishRawAsync(
                message.Topic,
                message.MessageKey,
                message.Payload,
                cancellationToken);

            message.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Outbox published EventType={EventType} Topic={Topic} Key={Key} OutboxId={OutboxId}",
                message.EventType,
                message.Topic,
                message.MessageKey,
                message.Id);
        }

        return pending.Count;
    }
}
