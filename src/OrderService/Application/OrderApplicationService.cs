using System.Text.Json;
using EdaDemo.Contracts;
using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Data;
using OrderService.Domain;
using OrderService.Dtos;

namespace OrderService.Application;

public sealed class OrderApplicationService : IOrderApplicationService
{
    private readonly OrderDbContext _db;
    private readonly KafkaOptions _kafka;
    private readonly ILogger<OrderApplicationService> _logger;

    public OrderApplicationService(
        OrderDbContext db,
        IOptions<KafkaOptions> kafka,
        ILogger<OrderApplicationService> logger)
    {
        _db = db;
        _kafka = kafka.Value;
        _logger = logger;
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var items = request.Items
            .Select(i => new OrderItem(i.ProductId, i.Quantity, i.UnitPrice))
            .ToList();
        var totalAmount = items.Sum(i => i.Quantity * i.UnitPrice);

        var order = new Order
        {
            Id = orderId,
            CustomerId = request.CustomerId.Trim(),
            TotalAmount = totalAmount,
            Status = OrderStatus.Created,
            CreatedAt = occurredAt,
            CorrelationId = correlationId,
            Items = items.Select(i => new OrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        var payload = new OrderCreated(
            eventId,
            correlationId,
            orderId,
            order.CustomerId,
            totalAmount,
            occurredAt,
            items);

        var envelope = EventEnvelope<OrderCreated>.Create(
            payload,
            EventTypes.OrderCreated,
            eventId,
            correlationId,
            occurredAt);

        // Outbox Pattern: the order and the event are committed together.
        // Kafka publish happens later from OutboxPublisherWorker so a crash
        // between "save" and "publish" cannot lose the event.
        var outbox = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = EventTypes.OrderCreated,
            Payload = JsonSerializer.Serialize(envelope, KafkaJson.Options),
            OccurredAt = occurredAt,
            Topic = _kafka.Topics.OrderEvents,
            MessageKey = orderId.ToString()
        };

        _db.Orders.Add(order);
        _db.OutboxMessages.Add(outbox);
        await _db.SaveChangesAsync(cancellationToken);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = orderId,
            ["CorrelationId"] = correlationId,
            ["EventId"] = eventId,
            ["EventType"] = EventTypes.OrderCreated,
            ["Service"] = "OrderService"
        }))
        {
            _logger.LogInformation(
                "OrderCreated OrderId={OrderId} CorrelationId={CorrelationId} EventId={EventId} TotalAmount={TotalAmount}",
                orderId,
                correlationId,
                eventId,
                totalAmount);
        }

        return new CreateOrderResponse
        {
            OrderId = orderId,
            Status = OrderStatus.Created
        };
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderResponse
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            CorrelationId = order.CorrelationId,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public static void Validate(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("customerId is required.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("At least one order item is required.");
        }

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId))
            {
                throw new ArgumentException("productId is required for every item.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("quantity must be greater than zero.");
            }

            if (item.UnitPrice < 0)
            {
                throw new ArgumentException("unitPrice cannot be negative.");
            }
        }
    }
}
