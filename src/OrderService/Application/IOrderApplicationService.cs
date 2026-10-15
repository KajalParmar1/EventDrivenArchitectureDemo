using OrderService.Dtos;

namespace OrderService.Application;

public interface IOrderApplicationService
{
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
