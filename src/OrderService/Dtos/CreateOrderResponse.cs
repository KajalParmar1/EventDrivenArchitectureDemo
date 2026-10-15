namespace OrderService.Dtos;

/// <summary>
/// Immediate response after an order is accepted. Downstream payment and inventory
/// work happens asynchronously through Kafka.
/// </summary>
public sealed class CreateOrderResponse
{
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid OrderId { get; set; }

    /// <example>Created</example>
    public string Status { get; set; } = "Created";
}
