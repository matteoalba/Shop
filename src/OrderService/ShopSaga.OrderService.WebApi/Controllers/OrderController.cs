using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderBusiness _orderBusiness;

        public OrderController(ILogger<OrderController> logger, IOrderBusiness orderBusiness)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderBusiness = orderBusiness ?? throw new ArgumentNullException(nameof(orderBusiness));
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<OrderDTO>> GetOrder(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per l'ordine con ID: {OrderId}", orderId);

                var order = await _orderBusiness.GetOrderByIdAsync(orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Ordine con ID {OrderId} non trovato", orderId);
                    return NotFound($"Ordine con ID {orderId} non trovato");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dell'ordine {OrderId}", orderId);
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per ottenere tutti gli ordini");

                var orders = await _orderBusiness.GetAllOrdersAsync(cancellationToken);

                if (orders == null || !orders.Any())
                {
                    _logger.LogWarning("Nessun ordine trovato");
                    return NotFound("Nessun ordine trovato");
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli ordini");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        [HttpGet("{customerId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrderByCustomerId(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per ottenere tutti gli ordini per il cliente con ID: {CustomerId}", customerId);

                var orders = await _orderBusiness.GetOrdersByCustomerIdAsync(customerId, cancellationToken);

                if (orders == null || !orders.Any())
                {
                    _logger.LogWarning("Nessun ordine trovato per il cliente con ID {CustomerId}", customerId);
                    return NotFound("Nessun ordine trovato per il cliente con ID " + customerId);
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli ordini");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        [HttpDelete("{orderId}")]
        public async Task<ActionResult<bool>> DeleteOrder(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per eliminare l'ordine con ID: {OrderId}", orderId);

                var result = await _orderBusiness.DeleteOrderAsync(orderId, cancellationToken);

                if (!result)
                {
                    _logger.LogWarning("Nessun ordine trovato per l'ID {OrderId}", orderId);
                    return NotFound("Nessun ordine trovato per l'ID " + orderId);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli ordini");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        [HttpPost]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] CreateOrderDTO createOrderDto, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per creare un nuovo ordine");
                
                if (createOrderDto?.OrderItems == null || !createOrderDto.OrderItems.Any())
                {
                    _logger.LogWarning("Tentativo di creare un ordine senza elementi");
                    return BadRequest("L'ordine deve contenere almeno un elemento");
                }

                var orderDto = new OrderDTO 
                {
                    OrderItems = createOrderDto.OrderItems.Select(item => new OrderItemDTO 
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList()
                };

                var result = await _orderBusiness.CreateOrderAsync(orderDto, cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Creazione dell'ordine fallita");
                    return BadRequest("Creazione dell'ordine fallita");
                }

                return CreatedAtAction(nameof(GetOrder), new { orderId = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dell'ordine");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        [HttpPut("{orderId}")]
        public async Task<ActionResult<OrderDTO>> UpdateOrder(int orderId, [FromBody] UpdateOrderDTO updateOrderDto, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per aggiornare l'ordine con ID: {OrderId}", orderId);
                
                if (updateOrderDto == null)
                {
                    _logger.LogWarning("Body della richiesta vuoto per l'aggiornamento dell'ordine {OrderId}", orderId);
                    return BadRequest("Il corpo della richiesta non può essere vuoto");
                }

                var orderDto = new OrderDTO 
                {
                    Id = orderId, 
                    Status = updateOrderDto.Status,
                    OrderItems = updateOrderDto.OrderItems?.Select(item => new OrderItemDTO
                    {
                        Id = item.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        ProductId = Guid.Empty
                    }).ToList() ?? new List<OrderItemDTO>()
                };
                
                var result = await _orderBusiness.UpdateOrderAsync(orderDto, cancellationToken);
                
                if (result == null)
                {
                    _logger.LogWarning("Aggiornamento dell'ordine {OrderId} fallito", orderId);
                    return NotFound($"Ordine con ID {orderId} non trovato o aggiornamento fallito");
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento dell'ordine {OrderId}", orderId);
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        [HttpPut("{orderId}/status")]
        public async Task<ActionResult<bool>> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderDTO updateOrderDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Ricevuta richiesta di aggiornamento stato per ordine {OrderId} a {Status}", 
                orderId, updateOrderDto?.Status);
            
            if (updateOrderDto == null || string.IsNullOrWhiteSpace(updateOrderDto.Status))
            {
                _logger.LogWarning("DTO di aggiornamento non valido per ordine {OrderId}", orderId);
                return BadRequest("Status is required");
            }
            
            try 
            {
                var result = await _orderBusiness.UpdateOrderStatusAsync(orderId, updateOrderDto.Status, cancellationToken);
                if (result == null)
                {
                    _logger.LogWarning("Ordine {OrderId} non trovato o aggiornamento fallito", orderId);
                    return NotFound(false);
                }
                
                _logger.LogInformation("Ordine {OrderId} aggiornato con successo a {Status}", orderId, result.Status);
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento dello stato dell'ordine {OrderId}", orderId);
                return StatusCode(500, false);
            }
        }

        [HttpPut("{orderId}/cancel")]
        public async Task<ActionResult<OrderDTO>> CancelOrder(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta per cancellare l'ordine con ID: {OrderId}", orderId);

                var result = await _orderBusiness.CancelOrderAsync(orderId, cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Ordine con ID {OrderId} non trovato o già cancellato", orderId);
                    return NotFound($"Ordine con ID {orderId} non trovato o non può essere cancellato");
                }

                _logger.LogInformation("Ordine {OrderId} cancellato con successo", orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione dell'ordine {OrderId}", orderId);
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }
    }
}
