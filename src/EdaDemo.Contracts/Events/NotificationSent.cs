namespace EdaDemo.Contracts;

public sealed record NotificationSent(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    DateTime OccurredAt);
