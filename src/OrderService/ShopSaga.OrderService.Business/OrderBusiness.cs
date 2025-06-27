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
            // Validazione input
            if (orderDto == null)
            {
                _logger.LogError("Tentativo di creare un ordine con un DTO nullo");
                return null;
            }

            if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
            {
                _logger.LogWarning("Tentativo di creare un ordine senza articoli");
                return null;
            }

            try
            {
                // Genera un nuovo GUID per il CustomerId
                var customerId = Guid.NewGuid();
                
                var orderItems = orderDto.OrderItems.Select(itemDto => new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice
                }).ToList();
                
                // Totale = (Quantity * UnitPrice) per ogni item
                var totalAmount = orderItems.Sum(item => item.Quantity * item.UnitPrice);
                
                // Mapping da OrderDTO a Order
                var order = new Order
                {
                    CustomerId = customerId,
                    Status = "Created", // Stato iniziale
                    TotalAmount = totalAmount, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                // Creazione dell'ordine nel repository
                var createdOrder = await _orderRepository.CreateOrderAsync(order, cancellationToken);
                
                // Salvataggio delle modifiche
                await _orderRepository.SaveChanges(cancellationToken);

                // Mapping dell'ordine creato per il DTO di ritorno
                var resultDto = MapOrderToDto(createdOrder);

                _logger.LogInformation("Ordine creato con successo con ID {OrderId}, CustomerId {CustomerId}", resultDto.Id, resultDto.CustomerId);
                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dell'ordine: {ErrorMessage}", ex.Message);
                return null;
            }
        }
        
        public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.DeleteOrderAsync(id, cancellationToken);
            if (!order)
            {
                _logger.LogWarning("Ordine con ID {OrderId} non trovato per la cancellazione", id);
                return false;
            }

            await _orderRepository.SaveChanges(cancellationToken);

            _logger.LogInformation("Ordine con ID {OrderId} cancellato con successo", id);
            return true;
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            var orders = await _orderRepository.GetAllOrdersAsync(cancellationToken);

            if (orders == null || !orders.Any())
                return Enumerable.Empty<OrderDTO>();

            return orders.Select(order => MapOrderToDto(order));
        }

        public async Task<OrderDTO> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id, cancellationToken);
            
            if (order == null)
                return null;
                
            // Mappa l'entità Order a OrderDTO
            return MapOrderToDto(order);
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
            if (orders == null || !orders.Any())
                return Enumerable.Empty<OrderDTO>();

            // Mappa l'entità Order a OrderDTO
            return orders.Select(order => MapOrderToDto(order));
        }

        public async Task<OrderDTO> UpdateOrderAsync(OrderDTO orderDto, CancellationToken cancellationToken = default)
        {
            if (orderDto == null)
            {
                _logger.LogError("Tentativo di aggiornare un ordine con un DTO nullo");
                return null;
            }

            if (orderDto.Id <= 0)
            {
                _logger.LogError("Tentativo di aggiornare un ordine con ID non valido: {OrderId}", orderDto.Id);
                return null;
            }

            // Controlla se tutti gli OrderItem con ID > 0 esistono nell'ordine
            if (orderDto.OrderItems != null && orderDto.OrderItems.Any())
            {
                var isValid = await _orderRepository.ValidateOrderItemsAsync(orderDto.Id, orderDto.OrderItems, cancellationToken);
                
                if (!isValid)
                {
                    _logger.LogWarning("Uno o più OrderItem specificati non esistono nell'ordine {OrderId}. Operazione annullata.", orderDto.Id);
                    return null;
                }
            }

            try
            {
                // Usa il repository per aggiornare l'ordine e gestire gli OrderItems
                var updatedOrder = await _orderRepository.UpdateOrderWithItemsAsync(
                    orderDto.Id, 
                    orderDto.Status, 
                    orderDto.OrderItems, 
                    cancellationToken);
                
                // Salva le modifiche
                await _orderRepository.SaveChanges(cancellationToken);
                
                // Mappa l'ordine aggiornato per il DTO di ritorno
                var resultDto = MapOrderToDto(updatedOrder);

                _logger.LogInformation("Ordine con ID {OrderId} aggiornato con successo", resultDto.Id);
                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento dell'ordine {OrderId}: {ErrorMessage}", orderDto.Id, ex.Message);
                return null;
            }
        }

        private OrderDTO MapOrderToDto(Order order)
        {
            if (order == null) return null;
            
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
                    Id = item.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList() ?? new List<OrderItemDTO>()
            };
        }

        public async Task<OrderDTO> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return new OrderDTO();
        }

        public async Task<SagaStateDTO> StartOrderProcessingAsync(int orderId, CancellationToken cancellationToken = default)
        {
            // TODO: Implementazione completa
            return new SagaStateDTO();
        }
    }
}
