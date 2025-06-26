using Microsoft.Extensions.Logging;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Repository.Model;
using ShopSaga.OrderService.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Business
{
    public class OrderBusiness : IOrderBusiness
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISagaStateRepository _sagaStateRepository;
        private readonly ILogger<OrderBusiness> _logger;
        
        public OrderBusiness(IOrderRepository orderRepository, ISagaStateRepository sagaStateRepository, ILogger<OrderBusiness> logger)
        {
            _sagaStateRepository = sagaStateRepository ?? throw new ArgumentNullException(nameof(sagaStateRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        }

        public async Task<OrderDTO> CreateOrderAsync(OrderDTO orderDto, CancellationToken cancellationToken = default)
        {
            if (orderDto == null)
                throw new ArgumentNullException(nameof(orderDto));
                
            // Validazione
            if (orderDto.CustomerId == Guid.Empty)
                throw new ArgumentException("CustomerId non può essere vuoto", nameof(orderDto));
                
            if (orderDto.OrderItems == null || orderDto.OrderItems.Count == 0)
                throw new ArgumentException("L'ordine deve contenere almeno un prodotto", nameof(orderDto));
                
            // Calcolo totale ordine
            decimal total = 0;
            foreach (var item in orderDto.OrderItems)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException($"La quantità deve essere maggiore di zero per il prodotto {item.ProductId}", nameof(orderDto));
                    
                if (item.UnitPrice <= 0)
                    throw new ArgumentException($"Il prezzo unitario deve essere maggiore di zero per il prodotto {item.ProductId}", nameof(orderDto));
                
                total += item.Quantity * item.UnitPrice;
            }
            
            orderDto.TotalAmount = total;
            orderDto.Status = "Created";
            orderDto.CreatedAt = DateTime.UtcNow;
            orderDto.UpdatedAt = DateTime.UtcNow;
            
            // Salva le modifiche nel database
            await _orderRepository.SaveChanges(cancellationToken);
            
            return orderDto;
        }
        
        public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return true;
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return new List<OrderDTO>();
        }

        public async Task<OrderDTO> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id, cancellationToken);
            
            if (order == null)
                return null;
                
            // Mappa l'entità Order a OrderDTO
            return new OrderDTO
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderItems = order.OrderItems?.Select(item => new OrderItemDTO
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList() ?? new List<OrderItemDTO>()
            };
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return new List<OrderDTO>();
        }

        public async Task<SagaStateDTO> StartOrderProcessingAsync(int orderId, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return new SagaStateDTO();
        }

        public async Task<OrderDTO> UpdateOrderAsync(OrderDTO orderDto, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return orderDto;
        }

        public async Task<OrderDTO> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return new OrderDTO();
        }
    }
}
