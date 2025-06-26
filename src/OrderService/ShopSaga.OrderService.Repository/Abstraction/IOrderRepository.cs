using ShopSaga.OrderService.Repository.Model;
using ShopSaga.OrderService.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Repository.Abstraction
{
    public interface IOrderRepository
    {
        Task<Order> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
        Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);
        Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChanges(CancellationToken cancellationToken = default);
        Task<Order> UpdateOrderWithItemsAsync(int orderId, string status, IEnumerable<OrderItemDTO> orderItems, CancellationToken cancellationToken = default);
        Task<List<int>> GetInvalidOrderItemIdsAsync(int orderId, IEnumerable<int> orderItemIds, CancellationToken cancellationToken = default);
        Task<bool> ValidateOrderItemsAsync(int orderId, IEnumerable<OrderItemDTO> orderItems, CancellationToken cancellationToken = default);
    }
}
