using Microsoft.AspNetCore.Mvc;
using ShopSaga.StockService.Business.Abstraction;
using ShopSaga.StockService.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.StockService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StockController : ControllerBase
    {
        private readonly ILogger<StockController> _logger;
        private readonly IStockBusiness _stockBusiness;

        public StockController(ILogger<StockController> logger, IStockBusiness stockBusiness)
        {
            _logger = logger;
            _stockBusiness = stockBusiness;
        }

        #region Gestione Prodotti

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllProducts(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _stockBusiness.GetAllProductsAsync(cancellationToken);
                if (products == null || !products.Any())
                    return NotFound("Nessun prodotto trovato");
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero di tutti i prodotti");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("products/{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _stockBusiness.GetProductAsync(id, cancellationToken);
                if (product == null)
                    return NotFound($"Prodotto con ID {id} non trovato");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero del prodotto {ProductId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("products/{productId}/availability")]
        public async Task<ActionResult<bool>> CheckProductAvailability(Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken = default)
        {
            try
            {
                var isAvailable = await _stockBusiness.IsProductAvailableAsync(productId, quantity, cancellationToken);
                if (quantity <= 0)
                    return BadRequest("La quantità richiesta deve essere maggiore di zero");
                return Ok(new { ProductId = productId, RequestedQuantity = quantity, IsAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel controllo disponibilità del prodotto {ProductId}", productId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("products/{productId}/stock")]
        public async Task<ActionResult<int>> GetAvailableStock(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                var stock = await _stockBusiness.GetAvailableStockAsync(productId, cancellationToken);
                return Ok(new { ProductId = productId, AvailableStock = stock });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dello stock per il prodotto {ProductId}", productId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPost("products")]
        public async Task<ActionResult<ProductDTO>> CreateProduct([FromBody] CreateProductDTO createProductDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var product = await _stockBusiness.CreateProductAsync(createProductDto, cancellationToken);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante la creazione del prodotto");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella creazione del prodotto");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPut("products/{id}")]
        public async Task<ActionResult<ProductDTO>> UpdateProduct(Guid id, [FromBody] UpdateProductDTO updateProductDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var product = await _stockBusiness.UpdateProductAsync(id, updateProductDto, cancellationToken);
                if (product == null)
                    return NotFound($"Prodotto con ID {id} non trovato");

                return Ok(product);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante l'aggiornamento del prodotto");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento del prodotto {ProductId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpDelete("products/{id}")]
        public async Task<ActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _stockBusiness.DeleteProductAsync(id, cancellationToken);
                if (!result)
                    return NotFound($"Prodotto con ID {id} non trovato");

                return Ok($"Prodotto con ID {id} eliminato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'eliminazione del prodotto {ProductId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        #endregion

        #region Gestione Stock Reservations

        [HttpGet("reservations/{id}")]
        public async Task<ActionResult<StockReservationDTO>> GetStockReservation(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var reservation = await _stockBusiness.GetStockReservationAsync(id, cancellationToken);
                if (reservation == null)
                    return NotFound($"Prenotazione stock con ID {id} non trovata");

                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero della prenotazione stock {ReservationId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpGet("reservations/order/{orderId}")]
        public async Task<ActionResult<IEnumerable<StockReservationDTO>>> GetStockReservationsByOrder(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reservations = await _stockBusiness.GetStockReservationsByOrderAsync(orderId, cancellationToken);
                return Ok(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero delle prenotazioni stock per l'ordine {OrderId}", orderId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPost("reservations")]
        public async Task<ActionResult<StockReservationDTO>> ReserveStock([FromBody] ReserveStockDTO reserveStockDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var reservation = await _stockBusiness.ReserveStockAsync(reserveStockDto, cancellationToken);
                return CreatedAtAction(nameof(GetStockReservation), new { id = reservation.Id }, reservation);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante la prenotazione dello stock");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante la prenotazione dello stock");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella prenotazione dello stock");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPost("reservations/batch")]
        public async Task<ActionResult<IEnumerable<StockReservationDTO>>> ReserveMultipleStock([FromBody] IEnumerable<ReserveStockDTO> reserveStockDtos, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var reservations = await _stockBusiness.ReserveMultipleStockAsync(reserveStockDtos, cancellationToken);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argomento non valido durante la prenotazione multipla dello stock");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante la prenotazione multipla dello stock");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella prenotazione multipla dello stock");
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPut("orders/{orderId}/reservations/confirm")]
        public async Task<ActionResult> ConfirmAllStockReservationsForOrder(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _stockBusiness.ConfirmAllStockReservationsForOrderAsync(orderId, cancellationToken);
                if (!result)
                    return NotFound($"Nessuna prenotazione stock trovata per l'ordine {orderId} o errore durante la conferma");

                return Ok($"Tutte le prenotazioni stock per l'ordine {orderId} sono state confermate con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella conferma delle prenotazioni stock per l'ordine {OrderId}", orderId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPut("reservations/{id}/cancel")]
        public async Task<ActionResult> CancelStockReservation(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _stockBusiness.CancelStockReservationAsync(id, cancellationToken);
                if (!result)
                    return NotFound($"Prenotazione stock con ID {id} non trovata");

                return Ok($"Prenotazione stock con ID {id} cancellata con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella cancellazione della prenotazione stock {ReservationId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        [HttpPut("reservations/order/{orderId}/cancel-all")]
        public async Task<ActionResult> CancelAllStockReservationsForOrder(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _stockBusiness.CancelAllStockReservationsForOrderAsync(orderId, cancellationToken);
                if (!result)
                    return BadRequest($"Errore nella cancellazione di alcune prenotazioni per l'ordine {orderId}");

                return Ok($"Tutte le prenotazioni stock per l'ordine {orderId} sono state cancellate con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella cancellazione di tutte le prenotazioni per l'ordine {OrderId}", orderId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        #endregion
    }
}
