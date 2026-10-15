namespace OrderService.Dtos;

/// <summary>
/// Line item on a new order.
/// </summary>
public sealed class CreateOrderItemRequest
{
    /// <example>product-001</example>
    public string ProductId { get; set; } = string.Empty;

    /// <example>2</example>
    public int Quantity { get; set; }

    /// <example>50.00</example>
    public decimal UnitPrice { get; set; }
}
