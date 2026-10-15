namespace EdaDemo.Contracts;

public sealed record PaymentProcessed(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    bool Success,
    DateTime OccurredAt);
