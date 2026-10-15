namespace EdaDemo.Contracts;

public sealed record OrderItem(
    string ProductId,
    int Quantity,
    decimal UnitPrice);
