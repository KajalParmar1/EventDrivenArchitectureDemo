namespace EdaDemo.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public string ConsumerGroup { get; set; } = string.Empty;
    public KafkaTopics Topics { get; set; } = new();

    public string DeadLetterTopic(string topic) => $"{topic}.dlq";
}

public sealed class KafkaTopics
{
    public string OrderEvents { get; set; } = "order-events";
    public string PaymentEvents { get; set; } = "payment-events";
    public string InventoryEvents { get; set; } = "inventory-events";
    public string NotificationEvents { get; set; } = "notification-events";
}
