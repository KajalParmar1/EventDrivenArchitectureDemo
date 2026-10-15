namespace OrderService.Domain;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string MessageKey { get; set; } = string.Empty;
}
