using EdaDemo.Contracts;
using EdaDemo.Messaging;

namespace NotificationService;

public sealed class NotificationProcessor
{
    public NotificationSent CreateNotification(Guid orderId, Guid correlationId)
    {
        return new NotificationSent(
            Guid.NewGuid(),
            correlationId,
            orderId,
            DateTime.UtcNow);
    }
}
