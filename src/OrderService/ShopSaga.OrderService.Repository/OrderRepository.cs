using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Repository.Model;
using ShopSaga.OrderService.Shared;
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
        
        public async Task<Order> UpdateOrderWithItemsAsync(int orderId, string status, IEnumerable<OrderItemDTO> orderItems, CancellationToken cancellationToken = default)
        {
            // Recupera l'ordine esistente con tutti gli items
            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
                
            if (existingOrder == null)
                throw new KeyNotFoundException($"Ordine con ID {orderId} non trovato");
            
            // Aggiorna lo stato dell'ordine
            existingOrder.Status = status;
            existingOrder.UpdatedAt = DateTime.UtcNow;
            
            if (orderItems != null)
            {
                // Ottieni tutti gli IDs degli item inclusi nella richiesta
                var requestedItemIds = orderItems
                    .Where(i => i.Id > 0)
                    .Select(i => i.Id)
                    .ToHashSet();
                
                // 1. Rimuovi gli item che non sono presenti nella richiesta
                var itemsToRemove = existingOrder.OrderItems
                    .Where(item => !requestedItemIds.Contains(item.Id))
                    .ToList();
                
                foreach (var itemToRemove in itemsToRemove)
                {
                    existingOrder.OrderItems.Remove(itemToRemove);
                    _context.Entry(itemToRemove).State = EntityState.Deleted;
                }
                
                // 2. Aggiorna gli item esistenti
                foreach (var itemDto in orderItems.Where(i => i.Id > 0))
                {
                    var existingItem = existingOrder.OrderItems.FirstOrDefault(i => i.Id == itemDto.Id);
                    
                    if (existingItem != null)
                    {
                        // Aggiorna solo le proprietà modificabili
                        existingItem.Quantity = itemDto.Quantity;
                        existingItem.UnitPrice = itemDto.UnitPrice;
                        // Non aggiorniamo ProductId per items esistenti
                        
                        _context.Entry(existingItem).State = EntityState.Modified;
                    }
                }
                
                // 3. Aggiungi nuovi item
                foreach (var newItemDto in orderItems.Where(i => i.Id == 0))
                {
                    var newItem = new OrderItem
                    {
                        OrderId = existingOrder.Id,
                        ProductId = Guid.NewGuid(), // Genera un nuovo ProductId
                        Quantity = newItemDto.Quantity,
                        UnitPrice = newItemDto.UnitPrice
                    };
                    
                    existingOrder.OrderItems.Add(newItem);
                }
                
                // 4. Ricalcola il totale dell'ordine
                existingOrder.TotalAmount = existingOrder.OrderItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            
            // Aggiorna l'ordine
            _context.Entry(existingOrder).State = EntityState.Modified;
            
            return existingOrder;
        }
        
        public async Task<List<int>> GetInvalidOrderItemIdsAsync(int orderId, IEnumerable<int> orderItemIds, CancellationToken cancellationToken = default)
        {
            // Ottieni tutti gli ID degli OrderItem che appartengono all'ordine specificato
            var validItemIds = await _context.OrderItems
                .Where(item => item.OrderId == orderId)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);

            // Restituisci gli ID che non appartengono all'ordine
            return orderItemIds.Where(id => !validItemIds.Contains(id)).ToList();
        }
        
        public async Task<bool> ValidateOrderItemsAsync(int orderId, IEnumerable<OrderItemDTO> orderItems, CancellationToken cancellationToken = default)
        {
            if (orderItems == null || !orderItems.Any())
                return true; // Non ci sono item da validare
                
            // Estrai gli ID degli item con ID > 0 (item esistenti)
            var itemIdsToCheck = orderItems
                .Where(i => i.Id > 0)
                .Select(i => i.Id)
                .ToList();
                
            if (!itemIdsToCheck.Any())
                return true; // Non ci sono item esistenti da validare
                
            // Controlla se ci sono ID non validi
            var invalidItemIds = await GetInvalidOrderItemIdsAsync(orderId, itemIdsToCheck, cancellationToken);
            
            return !invalidItemIds.Any(); // Restituisce true se non ci sono ID non validi
        }
    }
}
