using ShopSaga.StockService.Repository.Abstraction;
using ShopSaga.StockService.Repository.Model;
using ShopSaga.StockService.Shared;
using ShopSaga.StockService.Business.Abstraction;
using ShopSaga.OrderService.ClientHttp.Abstraction;
using ShopSaga.OrderService.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.OrderService.Shared.Events;

namespace ShopSaga.StockService.Business
{
    public class StockBusiness : IStockBusiness
    {
        private readonly IStockRepository _stockRepository;
        private readonly IOrderHttp _orderHttp;
        private readonly ILogger<StockBusiness> _logger;

        public StockBusiness(IStockRepository stockRepository, IOrderHttp orderHttp, ILogger<StockBusiness> logger)
        {
            _stockRepository = stockRepository;
            _orderHttp = orderHttp;
            _logger = logger;
        }

        #region Gestione Prodotti

        public async Task<ProductDTO> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var product = await _stockRepository.GetProductByIdAsync(id, cancellationToken);
            if (product == null)
                return null;
            return MapToProductDTO(product);
        }

        public async Task<IEnumerable<ProductDTO>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            var products = await _stockRepository.GetAllProductsAsync(cancellationToken);
            return products.Select(MapToProductDTO);
        }

        public async Task<ProductDTO> CreateProductAsync(CreateProductDTO createProductDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = new Product
                {
                    Name = createProductDto.Name,
                    Description = createProductDto.Description,
                    Price = createProductDto.Price,
                    QuantityInStock = createProductDto.QuantityInStock
                };
                var createdProduct = await _stockRepository.CreateProductAsync(product, cancellationToken);
                _logger.LogInformation("Prodotto creato con successo: {ProductName} (ID: {ProductId})", 
                    createdProduct.Name, createdProduct.Id);
                await _stockRepository.SaveChanges(cancellationToken);
                return MapToProductDTO(createdProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del prodotto: {ProductName}", createProductDto.Name);
                throw;
            }
        }

        public async Task<ProductDTO> UpdateProductAsync(Guid id, UpdateProductDTO updateProductDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingProduct = await _stockRepository.GetProductByIdAsync(id, cancellationToken);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Tentativo di aggiornamento di un prodotto inesistente: {ProductId}", id);
                    return null;
                }
                existingProduct.Name = updateProductDto.Name;
                existingProduct.Description = updateProductDto.Description;
                existingProduct.Price = updateProductDto.Price;
                existingProduct.QuantityInStock = updateProductDto.QuantityInStock;
                var updatedProduct = await _stockRepository.UpdateProductAsync(existingProduct, cancellationToken);
                _logger.LogInformation("Prodotto aggiornato con successo: {ProductName} (ID: {ProductId})", 
                    updatedProduct.Name, updatedProduct.Id);
                await _stockRepository.SaveChanges(cancellationToken);
                return MapToProductDTO(updatedProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento del prodotto: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _stockRepository.GetProductByIdAsync(id, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Tentativo di eliminazione di un prodotto inesistente: {ProductId}", id);
                    return false;
                }
                var result = await _stockRepository.DeleteProductAsync(id, cancellationToken);
                if (!result)
                {
                    _logger.LogWarning("Tentativo di eliminazione di un prodotto inesistente: {ProductId}", id);
                    return false;
                }
                _logger.LogInformation("Prodotto eliminato: {ProductName} (ID: {ProductId})", product.Name, id);
                await _stockRepository.SaveChanges(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione del prodotto: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> IsProductAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var product = await _stockRepository.GetProductByIdAsync(productId, cancellationToken);
            return product != null && product.QuantityInStock >= quantity;
        }

        public async Task<int> GetAvailableStockAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var product = await _stockRepository.GetProductByIdAsync(productId, cancellationToken);
            return product?.QuantityInStock ?? 0;
        }

        #endregion

        #region Gestione Stock Reservations

        public async Task<StockReservationDTO> ReserveStockAsync(ReserveStockDTO reserveStockDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _stockRepository.GetProductByIdAsync(reserveStockDto.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new ArgumentException($"Prodotto con ID {reserveStockDto.ProductId} non trovato");
                }
                if (product.QuantityInStock < reserveStockDto.Quantity)
                {
                    throw new InvalidOperationException($"Stock insufficiente per il prodotto '{product.Name}'. " +
                        $"Disponibile: {product.QuantityInStock}, Richiesto: {reserveStockDto.Quantity}");
                }
                var reservation = await _stockRepository.CreateStockReservationAsync(
                    reserveStockDto.OrderId, 
                    reserveStockDto.ProductId, 
                    reserveStockDto.Quantity, 
                    cancellationToken);
                _logger.LogInformation("Stock riservato con successo per l'ordine {OrderId}: {Quantity} unità del prodotto {ProductName}",
                    reserveStockDto.OrderId, reserveStockDto.Quantity, product.Name);
                await _stockRepository.SaveChanges(cancellationToken);
                return MapToStockReservationDTO(reservation, product.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la prenotazione dello stock per l'ordine {OrderId}", reserveStockDto.OrderId);
                throw;
            }
        }

        public async Task<bool> ConfirmAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reservations = await _stockRepository.GetStockReservationsByOrderIdAsync(orderId, cancellationToken);
                if (!reservations.Any())
                {
                    _logger.LogWarning("Nessuna prenotazione stock trovata per l'ordine {OrderId}", orderId);
                    return false;
                }
                var success = true;
                var confirmedCount = 0;
                foreach (var reservation in reservations)
                {
                    if (reservation.Status == "Reserved")
                    {
                        var result = await _stockRepository.ConfirmStockReservationAsync(reservation.Id, cancellationToken);
                        if (result)
                        {
                            confirmedCount++;
                            _logger.LogDebug("Prenotazione stock confermata: {ReservationId} per l'ordine {OrderId}", 
                                reservation.Id, orderId);
                        }
                        else
                        {
                            success = false;
                            _logger.LogWarning("Impossibile confermare la prenotazione stock {ReservationId} per l'ordine {OrderId}", 
                                reservation.Id, orderId);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Prenotazione stock {ReservationId} già in stato {Status}, saltata", 
                            reservation.Id, reservation.Status);
                    }
                }
                if (success && confirmedCount > 0)
                {
                    _logger.LogInformation("Tutte le prenotazioni stock per l'ordine {OrderId} sono state confermate con successo. Totale confermate: {Count}", 
                        orderId, confirmedCount);
                    try
                    {
                        // Aggiorna lo stato dell'ordine dopo la conferma delle prenotazioni
                        var statusUpdateResult = await _orderHttp.UpdateOrderStatusAsync(orderId, "StockConfirmed", cancellationToken);
                        if (statusUpdateResult)
                        {
                            _logger.LogInformation("Stato dell'ordine {OrderId} aggiornato a 'StockConfirmed'", orderId);
                        }
                        else
                        {
                            _logger.LogWarning("Impossibile aggiornare lo stato dell'ordine {OrderId} a 'StockConfirmed'", orderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore durante l'aggiornamento dello stato dell'ordine {OrderId} a 'StockConfirmed'", orderId);
                    }
                }
                else if (confirmedCount == 0)
                {
                    _logger.LogInformation("Nessuna prenotazione stock da confermare per l'ordine {OrderId} (potrebbero essere già confermate)", orderId);
                    success = true; 
                }
                await _stockRepository.SaveChanges(cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la conferma di tutte le prenotazioni stock per l'ordine {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CancelStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _stockRepository.CancelStockReservationAsync(reservationId, cancellationToken);
                if (result)
                {
                    _logger.LogInformation("Prenotazione stock cancellata con successo: {ReservationId}", reservationId);
                }
                else
                {
                    _logger.LogWarning("Impossibile cancellare la prenotazione stock: {ReservationId}", reservationId);
                }
                await _stockRepository.SaveChanges(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione della prenotazione stock: {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task<StockReservationDTO> GetStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await _stockRepository.GetStockReservationByIdAsync(reservationId, cancellationToken);
            if (reservation == null)
                return null;
            return MapToStockReservationDTO(reservation, reservation.Product?.Name ?? "Prodotto sconosciuto");
        }

        public async Task<IEnumerable<StockReservationDTO>> GetStockReservationsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var reservations = await _stockRepository.GetStockReservationsByOrderIdAsync(orderId, cancellationToken);
            return reservations.Select(r => MapToStockReservationDTO(r, r.Product?.Name ?? "Prodotto sconosciuto"));
        }

        public async Task<IEnumerable<StockReservationDTO>> ReserveMultipleStockAsync(IEnumerable<ReserveStockDTO> reserveStockDtos, CancellationToken cancellationToken = default)
        {
            var results = new List<StockReservationDTO>();
            var reservedItems = new List<Guid>();
            try
            {
                foreach (var reserveDto in reserveStockDtos)
                {
                    var reservation = await ReserveStockAsync(reserveDto, cancellationToken);
                    results.Add(reservation);
                    reservedItems.Add(reservation.Id);
                }
                _logger.LogInformation("Prenotazione multipla completata con successo per l'ordine {OrderId}: {Count} articoli",
                    reserveStockDtos.First().OrderId, results.Count);
                await _stockRepository.SaveChanges(cancellationToken);
                return results;
            }
            catch (Exception ex)
            {
                // Se qualcosa va storto, annulla tutte le prenotazioni già fatte
                _logger.LogError(ex, "Errore durante la prenotazione multipla. Annullamento delle prenotazioni già effettuate...");
                foreach (var reservationId in reservedItems)
                {
                    try
                    {
                        await CancelStockReservationAsync(reservationId, cancellationToken);
                    }
                    catch (Exception cancelEx)
                    {
                        _logger.LogError(cancelEx, "Errore durante l'annullamento della prenotazione {ReservationId}", reservationId);
                    }
                }
                throw;
            }
        }

        public async Task<bool> CancelAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reservations = await _stockRepository.GetStockReservationsByOrderIdAsync(orderId, cancellationToken);
                var success = true;
                foreach (var reservation in reservations)
                {
                    var result = await _stockRepository.CancelStockReservationAsync(reservation.Id, cancellationToken);
                    if (!result)
                    {
                        success = false;
                        _logger.LogWarning("Impossibile cancellare la prenotazione {ReservationId} per l'ordine {OrderId}", 
                            reservation.Id, orderId);
                    }
                }
                if (success)
                {
                    _logger.LogInformation("Tutte le prenotazioni stock per l'ordine {OrderId} sono state cancellate", orderId);
                    try
                    {
                        // Aggiorna solo lo stato dell'ordine dopo la cancellazione delle prenotazioni
                        var statusUpdateResult = await _orderHttp.UpdateOrderStatusAsync(orderId, "StockCancelled", cancellationToken);
                        if (statusUpdateResult)
                        {
                            _logger.LogInformation("Stato dell'ordine {OrderId} aggiornato a 'StockCancelled'", orderId);
                        }
                        else
                        {
                            _logger.LogWarning("Impossibile aggiornare lo stato dell'ordine {OrderId} a 'StockCancelled'", orderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore durante l'aggiornamento dello stato dell'ordine {OrderId} a 'StockCancelled'", orderId);
                    }
                }
                await _stockRepository.SaveChanges(cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione di tutte le prenotazioni per l'ordine {OrderId}", orderId);
                throw;
            }
        }

    public async Task ProcessOrderCreatedEventAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Elaborazione evento OrderCreated per ordine {OrderId}", orderCreatedEvent.OrderId);
            if (orderCreatedEvent.Items == null || !orderCreatedEvent.Items.Any())
            {
                _logger.LogWarning("Evento OrderCreated ricevuto senza articoli per l'ordine {OrderId}", orderCreatedEvent.OrderId);
                return;
            }
            // Prepara le richieste di prenotazione stock per ogni item dell'ordine
            var reserveStockDtos = orderCreatedEvent.Items.Select(item => new ReserveStockDTO
            {
                OrderId = orderCreatedEvent.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList();
            var reservationResults = await ReserveMultipleStockAsync(reserveStockDtos, cancellationToken);
            if (reservationResults == null || !reservationResults.Any())
            {
                _logger.LogWarning("Nessuna prenotazione stock creata da evento Kafka per l'ordine {OrderId}", orderCreatedEvent.OrderId);
                return;
            }
            _logger.LogInformation("Prenotazione stock completata da evento Kafka per l'ordine {OrderId}: {Count} prenotazioni create", 
                orderCreatedEvent.OrderId, reservationResults.Count());
            try 
            {
                // Dopo la prenotazione, aggiorna lo stato ordine tramite HTTP
                _logger.LogInformation("Tentativo di aggiornare lo stato dell'ordine {OrderId} a 'StockReserved'", orderCreatedEvent.OrderId);
                var success = await _orderHttp.UpdateOrderStatusAsync(orderCreatedEvent.OrderId, "StockReserved", cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Stato dell'ordine {OrderId} aggiornato a 'StockReserved' dopo la prenotazione dello stock", orderCreatedEvent.OrderId);
                }
                else
                {
                    _logger.LogWarning("Impossibile aggiornare lo stato dell'ordine {OrderId} a 'StockReserved' dopo la prenotazione dello stock", orderCreatedEvent.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento dello stato dell'ordine {OrderId} a 'StockReserved': {ErrorMessage}", 
                    orderCreatedEvent.OrderId, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'elaborazione dell'evento OrderCreated per l'ordine {OrderId}", orderCreatedEvent.OrderId);
        }
    }

        public async Task ProcessOrderCancelledEventAsync(OrderCancelledEvent orderCancelledEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Elaborazione evento OrderCancelled per ordine {OrderId}", orderCancelledEvent.OrderId);

                // Cancella tutte le prenotazioni stock per questo ordine
                var success = await CancelAllStockReservationsForOrderAsync(orderCancelledEvent.OrderId, cancellationToken);
                
                if (success)
                {
                    _logger.LogInformation("Tutte le prenotazioni stock per l'ordine {OrderId} sono state cancellate con successo", orderCancelledEvent.OrderId);
                    
                    try
                    {
                        // Conferma la cancellazione aggiornando lo stato dell'ordine
                        var statusUpdateResult = await _orderHttp.UpdateOrderStatusAsync(orderCancelledEvent.OrderId, "StockCancelled", cancellationToken);
                        if (statusUpdateResult)
                        {
                            _logger.LogInformation("Stato dell'ordine {OrderId} aggiornato a 'StockCancelled' dopo la cancellazione delle prenotazioni", orderCancelledEvent.OrderId);
                        }
                        else
                        {
                            _logger.LogWarning("Impossibile aggiornare lo stato dell'ordine {OrderId} a 'StockCancelled'", orderCancelledEvent.OrderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore durante l'aggiornamento dello stato dell'ordine {OrderId} a 'StockCancelled': {ErrorMessage}", 
                            orderCancelledEvent.OrderId, ex.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("Impossibile cancellare le prenotazioni stock per l'ordine {OrderId}", orderCancelledEvent.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione dell'evento OrderCancelled per l'ordine {OrderId}", orderCancelledEvent.OrderId);
            }
        }

        #endregion

        #region Metodi di Mapping

        private ProductDTO MapToProductDTO(Product product)
        {
            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                QuantityInStock = product.QuantityInStock,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        private StockReservationDTO MapToStockReservationDTO(StockReservation reservation, string productName)
        {
            return new StockReservationDTO
            {
                Id = reservation.Id,
                OrderId = reservation.OrderId,
                ProductId = reservation.ProductId,
                ProductName = productName,
                Quantity = reservation.Quantity,
                Status = reservation.Status,
                CreatedAt = reservation.CreatedAt,
                UpdatedAt = reservation.UpdatedAt
            };
        }

        #endregion
    }
}


