using EdaDemo.Contracts;

namespace InventoryService;

public sealed class InventoryReservationException : Exception
{
    public InventoryReservationException(string message) : base(message)
    {
    }
}

public sealed class InventoryProcessor
{
    private readonly int _failWhenTotalQuantityExceeds;

    public InventoryProcessor(int failWhenTotalQuantityExceeds)
    {
        _failWhenTotalQuantityExceeds = failWhenTotalQuantityExceeds;
    }

    public InventoryReserved Reserve(OrderCreated order)
    {
        var totalQuantity = order.Items.Sum(i => i.Quantity);
        if (totalQuantity > _failWhenTotalQuantityExceeds)
        {
            throw new InventoryReservationException(
                $"Simulated inventory failure for order {order.OrderId}: quantity {totalQuantity} exceeds {_failWhenTotalQuantityExceeds}.");
        }

        return new InventoryReserved(
            Guid.NewGuid(),
            order.CorrelationId,
            order.OrderId,
            Success: true,
            DateTime.UtcNow);
    }
}
