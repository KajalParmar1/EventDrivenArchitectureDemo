using EdaDemo.Contracts;
using PaymentService;

namespace PaymentService.Tests;

public class PaymentProcessorTests
{
    [Fact]
    public void Successful_payment_returns_PaymentProcessed()
    {
        var processor = new PaymentProcessor(failForOrdersAbove: 10000m);
        var order = CreateOrder(100m);

        var result = processor.Process(order);

        Assert.True(result.Success);
        Assert.Equal(order.OrderId, result.OrderId);
        Assert.Equal(order.CorrelationId, result.CorrelationId);
        Assert.NotEqual(Guid.Empty, result.EventId);
    }

    [Fact]
    public void Amount_above_threshold_simulates_payment_failure()
    {
        var processor = new PaymentProcessor(failForOrdersAbove: 10000m);
        var order = CreateOrder(10000.01m);

        var ex = Assert.Throws<PaymentProcessingException>(() => processor.Process(order));

        Assert.Contains("Simulated payment failure", ex.Message);
        Assert.Contains(order.OrderId.ToString(), ex.Message);
    }

    [Fact]
    public void Amount_equal_to_threshold_succeeds()
    {
        var processor = new PaymentProcessor(failForOrdersAbove: 10000m);
        var result = processor.Process(CreateOrder(10000m));

        Assert.True(result.Success);
    }

    private static OrderCreated CreateOrder(decimal total) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "customer-001",
            total,
            DateTime.UtcNow,
            [new OrderItem("product-001", 1, total)]);
}
