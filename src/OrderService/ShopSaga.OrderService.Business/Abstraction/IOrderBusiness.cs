using ShopSaga.OrderService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.OrderService.Shared;

namespace ShopSaga.OrderService.Business.Abstraction
{
    public interface IOrderBusiness
    {
        Task<OrderDTO> CreateOrderAsync(OrderDTO order, CancellationToken cancellationToken = default);
        Task<OrderDTO> UpdateOrderAsync(OrderDTO order, CancellationToken cancellationToken = default);
        Task<OrderDTO> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
        Task<OrderDTO> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default);
        Task<OrderDTO> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
