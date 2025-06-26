using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Add(order);
            return order;
        }

        public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders.FindAsync(new object[] { id }, cancellationToken);
            if (order == null)
                return false;

            _context.Orders.Remove(order);
            return true;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ToListAsync(cancellationToken);
        }

        public async Task<Order> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == customerId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            order.UpdatedAt = DateTime.UtcNow;
            _context.Entry(order).State = EntityState.Modified;
            return order;
        }
    }
}
