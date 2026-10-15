using EdaDemo.Contracts;
using InventoryService;

namespace InventoryService.Tests;

public class InventoryProcessorTests
{
    [Fact]
    public void Reserves_inventory_for_normal_quantities()
    {
        var processor = new InventoryProcessor(failWhenTotalQuantityExceeds: 1000);
        var order = CreateOrder(2);

        var result = processor.Reserve(order);

        Assert.True(result.Success);
        Assert.Equal(order.OrderId, result.OrderId);
        Assert.Equal(order.CorrelationId, result.CorrelationId);
    }

    [Fact]
    public void Quantity_above_threshold_simulates_reservation_failure()
    {
        var processor = new InventoryProcessor(failWhenTotalQuantityExceeds: 1000);
        var order = CreateOrder(1001);

        var ex = Assert.Throws<InventoryReservationException>(() => processor.Reserve(order));

        Assert.Contains("Simulated inventory failure", ex.Message);
    }

    private static OrderCreated CreateOrder(int quantity) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "customer-001",
            quantity * 10m,
            DateTime.UtcNow,
            [new OrderItem("product-001", quantity, 10m)]);
}
