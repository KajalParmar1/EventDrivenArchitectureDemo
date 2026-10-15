using EdaDemo.Contracts;

namespace PaymentService;

public sealed class PaymentProcessingException : Exception
{
    public PaymentProcessingException(string message) : base(message)
    {
    }
}

public sealed class PaymentProcessor
{
    private readonly decimal _failForOrdersAbove;

    public PaymentProcessor(decimal failForOrdersAbove)
    {
        _failForOrdersAbove = failForOrdersAbove;
    }

    public PaymentProcessed Process(OrderCreated order)
    {
        if (order.TotalAmount > _failForOrdersAbove)
        {
            throw new PaymentProcessingException(
                $"Simulated payment failure for order {order.OrderId}: amount {order.TotalAmount} exceeds {_failForOrdersAbove}.");
        }

        return new PaymentProcessed(
            Guid.NewGuid(),
            order.CorrelationId,
            order.OrderId,
            Success: true,
            DateTime.UtcNow);
    }
}
