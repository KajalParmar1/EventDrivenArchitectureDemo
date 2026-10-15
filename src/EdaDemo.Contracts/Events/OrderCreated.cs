namespace EdaDemo.Contracts;

public sealed record OrderCreated(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    DateTime OccurredAt,
    IReadOnlyCollection<OrderItem> Items);
