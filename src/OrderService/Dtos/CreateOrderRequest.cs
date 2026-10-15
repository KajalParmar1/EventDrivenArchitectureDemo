namespace OrderService.Dtos;

/// <summary>
/// Request body for creating an order. The order is persisted and an OrderCreated
/// event is written to the outbox in the same database transaction.
/// </summary>
public sealed class CreateOrderRequest
{
    /// <example>customer-001</example>
    public string CustomerId { get; set; } = string.Empty;

    public List<CreateOrderItemRequest> Items { get; set; } = [];
}
