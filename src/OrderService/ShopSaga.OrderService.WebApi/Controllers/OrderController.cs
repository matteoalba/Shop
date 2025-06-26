using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Shared;
using System;
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
        private readonly ISagaOrchestrator _sagaOrchestrator;

        public OrderController(ILogger<OrderController> logger, IOrderBusiness orderBusiness, ISagaOrchestrator sagaOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderBusiness = orderBusiness ?? throw new ArgumentNullException(nameof(orderBusiness));
            _sagaOrchestrator = sagaOrchestrator ?? throw new ArgumentNullException(nameof(sagaOrchestrator));
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
    }
}
