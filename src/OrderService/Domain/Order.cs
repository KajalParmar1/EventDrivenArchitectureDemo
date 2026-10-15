namespace OrderService.Domain;

public sealed class Order
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; set; }
    public Guid CorrelationId { get; set; }
    public List<OrderLine> Items { get; set; } = [];
}
