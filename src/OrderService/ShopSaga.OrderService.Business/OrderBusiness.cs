using Microsoft.Extensions.Logging;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Repository.Model;
using ShopSaga.OrderService.Shared;
using ShopSaga.OrderService.ClientHttp.Abstraction;
using ShopSaga.PaymentService.ClientHttp.Abstraction;
using ShopSaga.StockService.ClientHttp.Abstraction;
using ShopSaga.StockService.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.OrderService.Business.Kafka;
using ShopSaga.OrderService.Shared.Events;
using Microsoft.Extensions.Options;

namespace ShopSaga.OrderService.Business
{
    /// <summary>
    /// Orchestratore principale per la gestione degli ordini
    /// Coordina le operazioni tra servizi di stock, pagamento e ordini con compensazione automatica
    /// </summary>
    public class OrderBusiness : IOrderBusiness
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderBusiness> _logger;
        private readonly IPaymentHttp _paymentHttp;
        private readonly IStockHttp _stockHttp;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly KafkaSettings _kafkaSettings;
        
        public OrderBusiness(
            IOrderRepository orderRepository, 
            ILogger<OrderBusiness> logger, 
            IPaymentHttp paymentHttp, 
            IStockHttp stockHttp,
            IKafkaProducer kafkaProducer,
            IOptions<KafkaSettings> kafkaSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _paymentHttp = paymentHttp ?? throw new ArgumentNullException(nameof(paymentHttp));
            _stockHttp = stockHttp ?? throw new ArgumentNullException(nameof(stockHttp));
            _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
            _kafkaSettings = kafkaSettings.Value ?? throw new ArgumentNullException(nameof(kafkaSettings));
        }

        /// <summary>
        /// Implementa pattern SAGA per la creazione ordini: verifica stock → crea ordine → pubblica evento Kafk
        /// In caso di errore durante la pubblicazione Kafka, attiva compensazione automatica
        /// </summary>
        public async Task<OrderDTO> CreateOrderAsync(OrderDTO orderDto, CancellationToken cancellationToken = default)
        {
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
                _logger.LogInformation("Inizio creazione ordine con {ItemCount} articoli", orderDto.OrderItems.Count);

                var customerId = Guid.NewGuid();
                
                // Verifica preliminare disponibilità stock per tutti i prodotti
                foreach (var item in orderDto.OrderItems)
                {
                    var isAvailable = await _stockHttp.IsProductAvailableAsync(item.ProductId, item.Quantity, cancellationToken);
                    if (!isAvailable)
                    {
                        _logger.LogWarning("Prodotto {ProductId} non disponibile per quantità {Quantity}", item.ProductId, item.Quantity);
                        return null;
                    }
                }

                _logger.LogInformation("Tutti i prodotti sono disponibili, procedo con la creazione ordine");
                
                var orderItems = orderDto.OrderItems.Select(itemDto => new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice
                }).ToList();
                
                var totalAmount = orderItems.Sum(item => item.Quantity * item.UnitPrice);
                
                var order = new Order
                {
                    CustomerId = customerId,
                    Status = OrderStatus.Created,
                    TotalAmount = totalAmount, 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                var createdOrder = await _orderRepository.CreateOrderAsync(order, cancellationToken);
                await _orderRepository.SaveChanges(cancellationToken);

                _logger.LogInformation("Ordine {OrderId} creato, ora pubblico l'evento su Kafka", createdOrder.Id);

                // Pubblica evento OrderCreated su Kafka
                var eventOrderItems = orderDto.OrderItems.Select(item => new OrderItemEvent
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.UnitPrice
                }).ToList();
                
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    OrderId = createdOrder.Id,
                    CustomerId = createdOrder.CustomerId,
                    TotalAmount = createdOrder.TotalAmount,
                    Items = eventOrderItems
                };
                
                // Pubblicazione critica: se fallisce, l'ordine viene annullato (compensazione)
                var publishResult = await _kafkaProducer.ProduceAsync(
                    _kafkaSettings.OrderCreatedTopic,
                    $"order-{createdOrder.Id}",
                    orderCreatedEvent);
                
                if (!publishResult)
                {
                    _logger.LogError("Non è stato possibile pubblicare l'evento OrderCreated su Kafka per l'ordine {OrderId}. L'ordine verrà cancellato.", createdOrder.Id);
                    
                    // Compensazione automatica: elimina l'ordine creato
                    await _orderRepository.DeleteOrderAsync(createdOrder.Id, cancellationToken);
                    await _orderRepository.SaveChanges(cancellationToken);
                    
                    _logger.LogInformation("Ordine {OrderId} cancellato a causa dell'errore nella pubblicazione dell'evento Kafka", createdOrder.Id);
                    return null;
                }
                
                _logger.LogInformation("Evento OrderCreated pubblicato con successo per l'ordine {OrderId}. La prenotazione stock sarà gestita tramite Kafka.", createdOrder.Id);
                
                // Transizione di stato: ordine creato -> in attesa prenotazione stock
                createdOrder.Status = OrderStatus.StockPending;
                createdOrder.UpdatedAt = DateTime.UtcNow;
                await _orderRepository.SaveChanges(cancellationToken);


                var resultDto = MapOrderToDto(createdOrder);

                _logger.LogInformation("Ordine creato con successo con ID {OrderId}, CustomerId {CustomerId}, Status {Status}", 
                    resultDto.Id, resultDto.CustomerId, resultDto.Status);
                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dell'ordine: {ErrorMessage}", ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// Elimina un ordine con gestione intelligente di pagamenti e stock
        /// Blocca eliminazione per ordini con pagamenti completati
        /// </summary>
        public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            try 
            {
                _logger.LogInformation("Tentativo di cancellazione ordine {OrderId}", id);
                
                // Verifica che l'ordine esista
                var existingOrder = await _orderRepository.GetOrderByIdAsync(id, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Ordine con ID {OrderId} non trovato per la cancellazione", id);
                    return false;
                }
                
                // Verifica compatibilità con pagamenti esistenti
                bool hasCompletedPayment = false;
                try 
                {
                    var payment = await _paymentHttp.GetPaymentByOrderIdAsync(id, cancellationToken);
                    if (payment != null)
                    {
                        _logger.LogWarning("Impossibile cancellare ordine {OrderId}: pagamento esistente con stato {PaymentStatus}", 
                            id, payment.Status);
                        
                        // Blocco assoluto per pagamenti completati
                        if (payment.Status == "Completed")
                        {
                            _logger.LogError("Ordine {OrderId} non può essere cancellato: pagamento completato", id);
                            return false;
                        }
                        
                        if (payment.Status == "Pending")
                        {
                            _logger.LogWarning("Ordine {OrderId} ha un pagamento pending - procedendo con la cancellazione", id);
                        }
                        
                        hasCompletedPayment = payment.Status == "Completed";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Errore durante il controllo del pagamento per ordine {OrderId} - procedendo con la cancellazione", id);
                }
                
                // Libera le prenotazioni stock se il pagamento non è completato
                if (!hasCompletedPayment)
                {
                    try 
                    {
                        _logger.LogInformation("Rilascio stock reservations per ordine {OrderId}", id);
                        var stockReleased = await _stockHttp.CancelAllStockReservationsForOrderAsync(id, cancellationToken);
                        if (!stockReleased)
                        {
                            _logger.LogWarning("Errore nel rilascio stock reservations per ordine {OrderId}", id);
                        }
                        else
                        {
                            _logger.LogInformation("Stock reservations rilasciate con successo per ordine {OrderId}", id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore durante il rilascio stock reservations per ordine {OrderId}", id);
                    }
                }
                
                // Procedi con la cancellazione
                var deleted = await _orderRepository.DeleteOrderAsync(id, cancellationToken);
                if (!deleted)
                {
                    _logger.LogWarning("Errore durante la cancellazione dell'ordine {OrderId} dal repository", id);
                    return false;
                }

                await _orderRepository.SaveChanges(cancellationToken);

                _logger.LogInformation("Ordine con ID {OrderId} cancellato con successo", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione dell'ordine {OrderId}: {ErrorMessage}", id, ex.Message);
                return false;
            }
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

        /// <summary>
        /// Aggiorna un ordine con gestione completa degli items: add/update
        /// </summary>
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

            try
            {
                _logger.LogInformation("Inizio aggiornamento ordine {OrderId}", orderDto.Id);

                // Recupera l'ordine esistente con i suoi items
                var existingOrder = await _orderRepository.GetOrderByIdAsync(orderDto.Id, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Ordine {OrderId} non trovato per l'aggiornamento", orderDto.Id);
                    return null;
                }

                // Validazione integrità: tutti gli items devono appartenere all'ordine
                if (orderDto.OrderItems != null && orderDto.OrderItems.Any())
                {
                    var isValid = await _orderRepository.ValidateOrderItemsAsync(orderDto.Id, orderDto.OrderItems, cancellationToken);
                    
                    if (!isValid)
                    {
                        _logger.LogWarning("Uno o più OrderItem specificati non esistono nell'ordine {OrderId}. Operazione annullata.", orderDto.Id);
                        return null;
                    }
                }

                var currentItems = existingOrder.OrderItems.ToList();
                var newItems = orderDto.OrderItems?.ToList() ?? new List<OrderItemDTO>();

                _logger.LogInformation("Analisi modifiche ordine {OrderId}: {CurrentCount} items attuali, {NewCount} items nuovi", 
                    orderDto.Id, currentItems.Count, newItems.Count);

                var addedItems = newItems.Where(ni => ni.Id == 0 || !currentItems.Any(ci => ci.Id == ni.Id)).ToList();
                var updatedItems = newItems.Where(ni => ni.Id > 0 && currentItems.Any(ci => ci.Id == ni.Id && 
                    (ci.Quantity != ni.Quantity || ci.UnitPrice != ni.UnitPrice))).ToList();

                _logger.LogInformation("Modifiche ordine {OrderId}: {Added} aggiunti, {Updated} aggiornati", 
                    orderDto.Id, addedItems.Count, updatedItems.Count);

                // Orchestrazione SAGA per le modifiche di stock
                bool stockOperationsSuccessful = await HandleStockChangesAsync(orderDto.Id, addedItems, updatedItems, currentItems, cancellationToken);
                
                if (!stockOperationsSuccessful)
                {
                    _logger.LogError("Errore nelle operazioni di stock per ordine {OrderId}, rollback necessario", orderDto.Id);
                    return null;
                }

                var updatedOrder = await _orderRepository.UpdateOrderWithItemsAsync(
                    orderDto.Id, 
                    orderDto.Status ?? existingOrder.Status, 
                    orderDto.OrderItems, 
                    cancellationToken);
                
                await _orderRepository.SaveChanges(cancellationToken);
                
                var resultDto = MapOrderToDto(updatedOrder);

                _logger.LogInformation("Ordine con ID {OrderId} aggiornato con successo", resultDto.Id);
                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento dell'ordine {OrderId}: {ErrorMessage}", orderDto.Id, ex.Message);
                
                // Rollback di emergenza: cancella tutte le stock reservations
                try
                {
                    _logger.LogInformation("Avvio rollback stock reservations per ordine {OrderId}", orderDto.Id);
                    var rollbackSuccess = await _stockHttp.CancelAllStockReservationsForOrderAsync(orderDto.Id, cancellationToken);
                    
                    if (rollbackSuccess)
                    {
                        _logger.LogInformation("Rollback stock reservations completato per ordine {OrderId}", orderDto.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Rollback stock reservations parziale per ordine {OrderId}", orderDto.Id);
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Errore durante il rollback stock reservations per ordine {OrderId}", orderDto.Id);
                }
                
                return null;
            }
        }

        /// <summary>
        /// Gestisce modifiche stock con pattern SAGA e tracking operazioni per rollback
        /// Ogni operazione viene tracciata per permettere compensazione in caso di errore
        /// </summary>
        private async Task<bool> HandleStockChangesAsync(
            int orderId, 
            List<OrderItemDTO> addedItems, 
            List<OrderItemDTO> updatedItems, 
            List<OrderItem> currentItems, 
            CancellationToken cancellationToken)
        {
            var operationsExecuted = new List<Guid>(); // Tracking per rollback
            
            try
            {
                // Gestione items aggiunti: verifica disponibilità + prenotazione
                foreach (var addedItem in addedItems)
                {
                    _logger.LogInformation("Controllo disponibilità per nuovo item: Prodotto {ProductId}, Quantità {Quantity}", 
                        addedItem.ProductId, addedItem.Quantity);
                    
                    var isAvailable = await _stockHttp.IsProductAvailableAsync(addedItem.ProductId, addedItem.Quantity, cancellationToken);
                    if (!isAvailable)
                    {
                        _logger.LogWarning("Prodotto {ProductId} non disponibile per quantità {Quantity}", 
                            addedItem.ProductId, addedItem.Quantity);
                        await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                        return false;
                    }
                    
                    var reserveDto = new ReserveStockDTO
                    {
                        ProductId = addedItem.ProductId,
                        OrderId = orderId,
                        Quantity = addedItem.Quantity
                    };
                    
                    var reservation = await _stockHttp.ReserveStockAsync(reserveDto, cancellationToken);
                    if (reservation == null)
                    {
                        _logger.LogError("Errore nella creazione prenotazione stock per Prodotto {ProductId}", addedItem.ProductId);
                        await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                        return false;
                    }
                    
                    operationsExecuted.Add(reservation.Id);
                    _logger.LogInformation("Prenotazione stock creata per nuovo item: Prodotto {ProductId}, Reservation ID {ReservationId}", 
                        addedItem.ProductId, reservation.Id);
                }

                // Gestione items con quantità modificata
                foreach (var updatedItem in updatedItems)
                {
                    var currentItem = currentItems.First(ci => ci.Id == updatedItem.Id);
                    var quantityDelta = updatedItem.Quantity - currentItem.Quantity;

                    _logger.LogInformation("Item aggiornato: Prodotto {ProductId}, quantità delta {Delta}", 
                        updatedItem.ProductId, quantityDelta);

                    if (quantityDelta > 0)
                    {
                        // Aumentata quantità: controlla disponibilità per il delta
                        var isAvailable = await _stockHttp.IsProductAvailableAsync(updatedItem.ProductId, quantityDelta, cancellationToken);
                        if (!isAvailable)
                        {
                            _logger.LogWarning("Prodotto {ProductId} non disponibile per quantità aggiuntiva {Delta}", 
                                updatedItem.ProductId, quantityDelta);
                            await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                            return false;
                        }

                        // Creare un nuova prenotazione per il delta aggiuntivo
                        var reserveDto = new ReserveStockDTO
                        {
                            ProductId = updatedItem.ProductId,
                            OrderId = orderId,
                            Quantity = quantityDelta
                        };
                        
                        var deltaReservation = await _stockHttp.ReserveStockAsync(reserveDto, cancellationToken);
                        if (deltaReservation == null)
                        {
                            _logger.LogError("Errore nella creazione prenotazione aggiuntiva per Prodotto {ProductId}, Delta {Delta}", 
                                updatedItem.ProductId, quantityDelta);
                            await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                            return false;
                        }
                        
                        operationsExecuted.Add(deltaReservation.Id); 
                        _logger.LogInformation("Prenotazione stock aggiuntiva creata per Prodotto {ProductId}, Delta {Delta}, Reservation ID {ReservationId}", 
                            updatedItem.ProductId, quantityDelta, deltaReservation.Id);
                    }
                    else if (quantityDelta < 0)
                    {
                        // Diminuita quantità: riduci la prenotazione esistente
                        var existingReservations = await _stockHttp.GetStockReservationsByOrderAsync(orderId, cancellationToken);
                        var reservationToReduce = existingReservations.Where(r => r.ProductId == updatedItem.ProductId).FirstOrDefault();
                        
                        if (reservationToReduce != null)
                        {
                            var cancelled = await _stockHttp.CancelStockReservationAsync(reservationToReduce.Id, cancellationToken);
                            if (!cancelled)
                            {
                                _logger.LogError("Errore nella cancellazione prenotazione esistente per Prodotto {ProductId}", updatedItem.ProductId);
                                await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                                return false;
                            }
                            
                            // Crea nuova prenotazione con quantità aggiornata
                            var newReserveDto = new ReserveStockDTO
                            {
                                ProductId = updatedItem.ProductId,
                                OrderId = orderId,
                                Quantity = updatedItem.Quantity
                            };
                            
                            var newReservation = await _stockHttp.ReserveStockAsync(newReserveDto, cancellationToken);
                            if (newReservation == null)
                            {
                                _logger.LogError("Errore nella ricreazione prenotazione per Prodotto {ProductId}, Nuova quantità {Quantity}", 
                                    updatedItem.ProductId, updatedItem.Quantity);
                                await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                                return false;
                            }
                            
                            operationsExecuted.Add(newReservation.Id);
                            _logger.LogInformation("Prenotazione stock ridotta per Prodotto {ProductId}, Nuova quantità {Quantity}, Reservation ID {ReservationId}", 
                                updatedItem.ProductId, updatedItem.Quantity, newReservation.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Nessuna prenotazione esistente trovata per Prodotto {ProductId} dell'ordine {OrderId}", 
                                updatedItem.ProductId, orderId);
                        }
                    }
                }

                _logger.LogInformation("Tutte le operazioni stock per ordine {OrderId} completate con successo", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante le operazioni stock per ordine {OrderId}", orderId);
                await RollbackOperations(orderId, operationsExecuted, cancellationToken);
                return false;
            }
        }
        
        private async Task RollbackOperations(int orderId, List<Guid> operationsExecuted, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Avvio rollback per {Count} operazioni stock dell'ordine {OrderId}", operationsExecuted.Count, orderId);
            
            // Rollback semplice: cancella tutte le reservations create durante questa operazione
            foreach (var reservationId in operationsExecuted)
            {
                try
                {
                    await _stockHttp.CancelStockReservationAsync(reservationId, cancellationToken);
                    _logger.LogInformation("Rollback: cancellata reservation {ReservationId}", reservationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante rollback reservation {ReservationId}", reservationId);
                }
            }
            
            _logger.LogInformation("Rollback completato per ordine {OrderId}", orderId);
        }

        // Mapping
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
            try
            {
                _logger.LogInformation("Aggiornamento stato ordine {OrderId} a {Status}", orderId, status);

                if (orderId <= 0)
                {
                    _logger.LogError("ID ordine non valido: {OrderId}", orderId);
                    return null;
                }
                
                if (string.IsNullOrWhiteSpace(status))
                {
                    _logger.LogError("Status non valido per ordine {OrderId}", orderId);
                    return null;
                }
     
                var existingOrder = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Ordine {OrderId} non trovato per aggiornamento status", orderId);
                    return null;
                }
                
                _logger.LogInformation("Ordine {OrderId}: cambio stato da '{OldStatus}' a '{NewStatus}'", 
                    orderId, existingOrder.Status, status);
                
                // Aggiorna solo lo status
                existingOrder.Status = status;
                existingOrder.UpdatedAt = DateTime.UtcNow;

                await _orderRepository.SaveChanges(cancellationToken);
                
                var resultDto = MapOrderToDto(existingOrder);
                
                _logger.LogInformation("Status ordine {OrderId} aggiornato con successo a '{Status}'", orderId, status);
                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento status ordine {OrderId} a '{Status}': {ErrorMessage}", 
                    orderId, status, ex.Message);
                return null;
            }
        }

        public async Task<OrderDTO> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Inizio cancellazione ordine {OrderId}", orderId);

                var payment = await _paymentHttp.GetPaymentByOrderIdAsync(orderId, cancellationToken);
                if (payment != null)
                {
                    _logger.LogWarning("Impossibile cancellare ordine {OrderId}: esiste già un pagamento con stato {PaymentStatus}", orderId, payment.Status);
                    return null;
                }

                var existingOrder = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Ordine {OrderId} non trovato per la cancellazione", orderId);
                    return null;
                }

                // Verifica se l'ordine può essere cancellato
                if (existingOrder.Status == OrderStatus.Cancelled)
                {
                    _logger.LogWarning("Ordine {OrderId} è già stato cancellato", orderId);
                    return MapOrderToDto(existingOrder);
                }

                if (existingOrder.Status == OrderStatus.Completed)
                {
                    _logger.LogWarning("Impossibile cancellare ordine {OrderId}: ordine già completato", orderId);
                    return null;
                }

                // Aggiorna lo status a "Cancelled"
                existingOrder.Status = OrderStatus.Cancelled;
                existingOrder.UpdatedAt = DateTime.UtcNow;

                await _orderRepository.SaveChanges(cancellationToken);

                _logger.LogInformation("Ordine {OrderId} aggiornato a status 'Cancelled'", orderId);

                // Crea l'evento di cancellazione per Kafka
                var orderCancelledEvent = new OrderCancelledEvent
                {
                    OrderId = orderId,
                    CustomerId = existingOrder.CustomerId,
                    CancelReason = "Cancellazione richiesta dall'utente",
                    Items = existingOrder.OrderItems.Select(item => new OrderItemEvent
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.UnitPrice
                    }).ToList(),
                    Timestamp = DateTime.UtcNow
                };

                // Pubblica l'evento di cancellazione su Kafka
                try
                {
                    _logger.LogInformation("Pubblicazione evento OrderCancelled su Kafka per ordine {OrderId}", orderId);
                    await _kafkaProducer.ProduceAsync(_kafkaSettings.OrderCancelledTopic, $"order-{orderId}", orderCancelledEvent);
                    _logger.LogInformation("Evento OrderCancelled pubblicato con successo per ordine {OrderId}", orderId);
                }
                catch (Exception kafkaEx)
                {
                    _logger.LogError(kafkaEx, "Errore durante la pubblicazione dell'evento OrderCancelled per ordine {OrderId}: {ErrorMessage}", 
                        orderId, kafkaEx.Message);
                }

                _logger.LogInformation("Cancellazione ordine {OrderId} completata con successo", orderId);
                return MapOrderToDto(existingOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione dell'ordine {OrderId}: {ErrorMessage}", 
                    orderId, ex.Message);
                return null;
            }
        }
    }
}
