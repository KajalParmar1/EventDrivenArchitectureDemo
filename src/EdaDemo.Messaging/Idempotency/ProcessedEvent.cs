namespace EdaDemo.Messaging;

public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
