using Microsoft.AspNetCore.Mvc;
using OrderService.Application;
using OrderService.Dtos;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderApplicationService _orders;

    public OrdersController(IOrderApplicationService orders)
    {
        _orders = orders;
    }

    /// <summary>
    /// Creates an order, stores it in PostgreSQL, and records an OrderCreated event in the outbox.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/orders
    ///     {
    ///        "customerId": "customer-001",
    ///        "items": [
    ///           {
    ///              "productId": "product-001",
    ///              "quantity": 2,
    ///              "unitPrice": 50.00
    ///           }
    ///        ]
    ///     }
    ///
    /// Sample response:
    ///
    ///     {
    ///        "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "status": "Created"
    ///     }
    ///
    /// The HTTP call returns as soon as the order is saved. Payment, inventory, and
    /// notification processing happen asynchronously through Kafka.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateOrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orders.CreateOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Returns a previously created order by id.
    /// </summary>
    /// <remarks>
    /// Sample response:
    ///
    ///     {
    ///        "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "customerId": "customer-001",
    ///        "totalAmount": 100.00,
    ///        "status": "Created",
    ///        "createdAt": "2026-08-29T12:00:00Z",
    ///        "correlationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///        "items": [
    ///           {
    ///              "productId": "product-001",
    ///              "quantity": 2,
    ///              "unitPrice": 50.00
    ///           }
    ///        ]
    ///     }
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetOrderAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}
