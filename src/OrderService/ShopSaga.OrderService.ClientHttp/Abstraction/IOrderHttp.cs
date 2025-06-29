using ShopSaga.OrderService.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.ClientHttp.Abstraction
{
    public interface IOrderHttp
    {
        Task<OrderDTO?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<OrderDTO?> CreateOrderAsync(CreateOrderDTO createOrderDto, CancellationToken cancellationToken = default);
        Task<OrderDTO?> UpdateOrderAsync(int orderId, UpdateOrderDTO updateOrderDto, CancellationToken cancellationToken = default);
        Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default);
    }
}