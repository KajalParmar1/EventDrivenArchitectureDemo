namespace OrderService.Dtos;

public sealed class OrderItemResponse
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class OrderResponse
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid CorrelationId { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
}
