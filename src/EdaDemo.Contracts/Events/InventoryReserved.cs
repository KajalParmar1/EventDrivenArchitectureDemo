namespace EdaDemo.Contracts;

public sealed record InventoryReserved(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    bool Success,
    DateTime OccurredAt);
