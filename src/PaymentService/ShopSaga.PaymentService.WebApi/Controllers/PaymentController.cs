using Microsoft.AspNetCore.Mvc;
using ShopSaga.PaymentService.Business.Abstraction;
using ShopSaga.PaymentService.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.PaymentService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaymentBusiness _paymentBusiness;

        public PaymentController(ILogger<PaymentController> logger, IPaymentBusiness paymentBusiness)
        {
            _logger = logger;
            _paymentBusiness = paymentBusiness;
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDTO>> GetPayment(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var payment = await _paymentBusiness.GetPaymentAsync(id, cancellationToken);
                if (payment == null)
                    return NotFound($"Pagamento con ID {id} non trovato");

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero del pagamento {PaymentId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<PaymentDTO>> GetPaymentByOrderId(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var payment = await _paymentBusiness.GetPaymentByOrderIdAsync(orderId, cancellationToken);
                if (payment == null)
                    return NotFound($"Pagamento per l'ordine {orderId} non trovato");

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero del pagamento per l'ordine {OrderId}", orderId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetAllPayments(CancellationToken cancellationToken = default)
        {
            try
            {
                var payments = await _paymentBusiness.GetAllPaymentsAsync(cancellationToken);
                if (payments == null || !payments.Any())
                    return NotFound("Nessun pagamento trovato");
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero di tutti i pagamenti");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetPaymentsByStatus(string status, CancellationToken cancellationToken = default)
        {
            try
            {   
                var payments = await _paymentBusiness.GetPaymentsByStatusAsync(status, cancellationToken);
                if (payments == null || !payments.Any())
                    return NotFound($"Nessun pagamento trovato con stato '{status}'");
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dei pagamenti per stato {Status}", status);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("order/{orderId}/processed")]
        public async Task<ActionResult<bool>> IsPaymentProcessed(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var isProcessed = await _paymentBusiness.IsPaymentProcessedAsync(orderId, cancellationToken);
                return Ok(isProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel controllo se il pagamento è elaborato per l'ordine {OrderId}", orderId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPost]
        public async Task<ActionResult<PaymentDTO>> CreatePayment([FromBody] CreatePaymentDTO createPaymentDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var payment = await _paymentBusiness.CreatePaymentAsync(createPaymentDto, cancellationToken);
                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante la creazione del pagamento");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella creazione del pagamento");
                return StatusCode(500, "Errore interno del server");
            }
        }

                [HttpPost("refund")]
        public async Task<ActionResult<PaymentDTO>> RefundPayment([FromBody] RefundPaymentDTO refundPaymentDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var payment = await _paymentBusiness.RefundPaymentAsync(refundPaymentDto, cancellationToken);
                return Ok(payment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante il rimborso del pagamento");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante il rimborso del pagamento");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel rimborso del pagamento");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePayment(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _paymentBusiness.DeletePaymentAsync(id, cancellationToken);
                if (!result)
                    return NotFound($"Pagamento con ID {id} non trovato");

                return Ok($"Pagamento con ID {id} cancellato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella cancellazione del pagamento {PaymentId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPut("{orderId}")]
        public async Task<ActionResult<PaymentDTO>> ProcessPayment(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var payment = await _paymentBusiness.ProcessPaymentAsync(orderId, cancellationToken);
                return Ok(payment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante l'elaborazione del pagamento");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante l'elaborazione del pagamento");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del pagamento");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPut("{paymentId}")]
        public async Task<ActionResult<PaymentDTO>> CancelPayment(int paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var payment = await _paymentBusiness.CancelPaymentAsync(paymentId, cancellationToken);
                if (payment == null)
                    return NotFound($"Pagamento con ID {paymentId} non trovato");

                return Ok(payment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante l'annullamento del pagamento");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante l'annullamento del pagamento");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'annullamento del pagamento");
                return StatusCode(500, "Errore interno del server");
            }
        }
    }
}
