using ShopSaga.PaymentService.Repository.Abstraction;
using ShopSaga.PaymentService.Repository.Model;
using ShopSaga.PaymentService.Shared;
using ShopSaga.PaymentService.Business.Abstraction;
using ShopSaga.OrderService.ClientHttp.Abstraction;
using ShopSaga.StockService.ClientHttp.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.PaymentService.Business
{
    public class PaymentBusiness : IPaymentBusiness
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderHttp _orderHttp;
        private readonly IStockHttp _stockHttp;
        private readonly ILogger<PaymentBusiness> _logger;

        public PaymentBusiness(IPaymentRepository paymentRepository, IOrderHttp orderHttp, IStockHttp stockHttp, ILogger<PaymentBusiness> logger)
        {
            _paymentRepository = paymentRepository;
            _orderHttp = orderHttp;
            _stockHttp = stockHttp;
            _logger = logger;
        }

        public async Task<PaymentDTO> GetPaymentAsync(int id, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentRepository.GetPaymentByIdAsync(id, cancellationToken);
            if (payment == null)
                return null;
            return  MapToDTO(payment);
        }

        public async Task<PaymentDTO> CreatePaymentAsync(CreatePaymentDTO createPaymentDto, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateCreatePaymentDto(createPaymentDto);

                // 1. Verifica che l'ordine esista
                _logger.LogInformation("Verifica esistenza ordine {OrderId} prima di creare il pagamento", createPaymentDto.OrderId);
                var order = await _orderHttp.GetOrderAsync(createPaymentDto.OrderId, cancellationToken);
                
                if (order == null)
                {
                    _logger.LogWarning("Tentativo di creare pagamento per ordine inesistente: {OrderId}", createPaymentDto.OrderId);
                    throw new ArgumentException($"Ordine con ID {createPaymentDto.OrderId} non trovato");
                }

                // 2. Verifica che l'importo corrisponda
                if (order.TotalAmount != createPaymentDto.Amount)
                {
                    _logger.LogWarning("Importo pagamento non corrisponde. Ordine: {OrderAmount}, Richiesto: {PaymentAmount}", 
                        order.TotalAmount, createPaymentDto.Amount);
                    throw new ArgumentException($"L'importo del pagamento ({createPaymentDto.Amount}) non corrisponde al totale dell'ordine ({order.TotalAmount})");
                }

                // 3. Verifica che l'ordine sia in stato valido per il pagamento
                var validStatuses = new[] { "StockReserved" }; // Solo ordini con stock confermato
                if (!validStatuses.Contains(order.Status))
                {
                    _logger.LogWarning("Ordine {OrderId} in stato non valido per pagamento: {Status}", createPaymentDto.OrderId, order.Status);
                    throw new InvalidOperationException($"Ordine {createPaymentDto.OrderId} non è in stato valido per il pagamento. Stato attuale: {order.Status}. Stato richiesto: StockReserved");
                }

                // 4. Verifica che non esista già un pagamento per questo ordine
                var existingPayment = await _paymentRepository.GetPaymentByOrderIdAsync(createPaymentDto.OrderId, cancellationToken);
                if (existingPayment != null)
                {
                    _logger.LogWarning("Tentativo di creare pagamento duplicato per ordine: {OrderId}", createPaymentDto.OrderId);
                    throw new InvalidOperationException($"Esiste già un pagamento per l'ordine {createPaymentDto.OrderId}");
                }

                var payment = new Payment
                {
                    OrderId = createPaymentDto.OrderId,
                    Amount = createPaymentDto.Amount,
                    PaymentMethod = createPaymentDto.PaymentMethod,
                    Status = "Pending"
                };

                var createdPayment = await _paymentRepository.CreatePaymentAsync(payment, cancellationToken);
                await _paymentRepository.SaveChanges(cancellationToken);

                _logger.LogInformation("Pagamento creato con successo per ordine {OrderId} con importo {Amount}", 
                    createPaymentDto.OrderId, createPaymentDto.Amount);

                return MapToDTO(createdPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del pagamento per ordine {OrderId}", createPaymentDto.OrderId);
                throw;
            }
        }

        public async Task<bool> DeletePaymentAsync(int id, CancellationToken cancellationToken = default)
        {
            var result = await _paymentRepository.DeletePaymentAsync(id, cancellationToken);
            if (result)
                await _paymentRepository.SaveChanges(cancellationToken);
            
            return result;
        }

        public async Task<PaymentDTO> GetPaymentByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentRepository.GetPaymentByOrderIdAsync(orderId, cancellationToken);
            return payment != null ? MapToDTO(payment) : null;
        }

        public async Task<IEnumerable<PaymentDTO>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(status))
                throw new ArgumentException("Lo status non può essere nullo o vuoto", nameof(status));

            var payments = await _paymentRepository.GetPaymentsByStatusAsync(status, cancellationToken);
            return payments.Select(MapToDTO);
        }

        public async Task<IEnumerable<PaymentDTO>> GetAllPaymentsAsync(CancellationToken cancellationToken = default)
        {
            var payments = await _paymentRepository.GetAllPaymentsAsync(cancellationToken);
            return payments.Select(MapToDTO);
        }

        public async Task<PaymentDTO> ProcessPaymentAsync(int orderId, CancellationToken cancellationToken = default)
        {
            if (orderId <= 0)
                throw new ArgumentException("OrderId è richiesto e deve essere maggiore di 0", nameof(orderId));

            // Validazione preliminare: verifica che il pagamento è stato creato
            var existingPayment = await _paymentRepository.GetPaymentByOrderIdAsync(orderId, cancellationToken);
            if (existingPayment == null)
            {
                _logger.LogWarning("Tentativo di processare pagamento per ordine inesistente: {OrderId}", orderId);
                throw new InvalidOperationException($"Nessun pagamento trovato per l'ordine {orderId}");
            }

            try
            {
                _logger.LogInformation("SAGA: Avvio processo pagamento per ordine {OrderId}", orderId);
                
                // STEP 1: Processa il pagamento internamente
                var payment = await _paymentRepository.ProcessPaymentAsync(orderId, cancellationToken);
                await _paymentRepository.SaveChanges(cancellationToken);
                
                if (payment == null || payment.Status != "Completed")
                {
                    _logger.LogWarning("SAGA: Pagamento fallito per ordine {OrderId}", orderId);
                    
                    // Se il pagamento fallisce, cancella le prenotazioni stock
                    await _stockHttp.CancelAllStockReservationsForOrderAsync(orderId, cancellationToken);
                    
                    // Aggiorna stato ordine a PaymentFailed
                    await _orderHttp.UpdateOrderStatusAsync(orderId, "PaymentFailed", cancellationToken);
                    
                    return MapToDTO(payment);
                }
                
                _logger.LogInformation("SAGA: Pagamento completato per ordine {OrderId}", orderId);
                
                // STEP 2: Conferma definitivamente tutte le prenotazioni stock (POST-PIVOT)
                var stockConfirmed = await _stockHttp.ConfirmAllStockReservationsForOrderAsync(orderId, cancellationToken);
                
                if (stockConfirmed)
                {
                    _logger.LogInformation("SAGA: Stock confermato definitivamente per ordine {OrderId}", orderId);
                    
                    // STEP 3: Aggiorna stato ordine a PaymentCompleted
                    await _orderHttp.UpdateOrderStatusAsync(orderId, "PaymentCompleted", cancellationToken);
                }
                else
                {
                    _logger.LogError("SAGA: Errore critico - Pagamento completato ma stock non confermato per ordine {OrderId}", orderId);
                    
                    // Scenario critico: Pagamento OK ma stock fallito
                    // Non possiamo fare rollback del pagamento, serve intervento manuale
                    await _orderHttp.UpdateOrderStatusAsync(orderId, "ManualIntervention", cancellationToken);
                }
                
                return MapToDTO(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAGA: Errore durante processo pagamento ordine {OrderId}", orderId);
                
                // In caso di errore, assicurati che le prenotazioni stock siano cancellate
                try
                {
                    await _stockHttp.CancelAllStockReservationsForOrderAsync(orderId, cancellationToken);
                    await _orderHttp.UpdateOrderStatusAsync(orderId, "PaymentFailed", cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "SAGA: Errore durante cleanup per ordine {OrderId}", orderId);
                }
                
                throw;
            }
        }

        public async Task<PaymentDTO> RefundPaymentAsync(RefundPaymentDTO refundPaymentDto, CancellationToken cancellationToken = default)
        {
            ValidateRefundPaymentDto(refundPaymentDto);

            try
            {
                _logger.LogInformation("SAGA: Avvio rimborso per payment {PaymentId}", refundPaymentDto.PaymentId);
                
                // STEP 1: Effettua il rimborso internamente
                var refundPayment = await _paymentRepository.RefundPaymentAsync(
                    refundPaymentDto.PaymentId,
                    refundPaymentDto.Amount,
                    refundPaymentDto.Reason,
                    cancellationToken);

                await _paymentRepository.SaveChanges(cancellationToken);
                
                if (refundPayment == null)
                {
                    _logger.LogWarning("SAGA: Rimborso fallito per payment {PaymentId}", refundPaymentDto.PaymentId);
                    return null;
                }
                
                _logger.LogInformation("SAGA: Rimborso completato per payment {PaymentId}, ordine {OrderId}", 
                    refundPayment.Id, refundPayment.OrderId);
                
                // STEP 2: Rilascia tutto lo stock confermato (COMPENSAZIONE)
                var stockReleased = await _stockHttp.CancelAllStockReservationsForOrderAsync(refundPayment.OrderId, cancellationToken);
                
                if (stockReleased)
                {
                    _logger.LogInformation("SAGA: Stock rilasciato per ordine {OrderId} dopo rimborso", refundPayment.OrderId);
                }
                else
                {
                    _logger.LogWarning("SAGA: Rimborso OK ma errore rilascio stock per ordine {OrderId}", refundPayment.OrderId);
                }
                
                // STEP 3: Aggiorna stato ordine a Refunded
                await _orderHttp.UpdateOrderAsync(refundPayment.OrderId, new ShopSaga.OrderService.Shared.UpdateOrderDTO
                {
                    Status = "Refunded"
                }, cancellationToken);
                
                return MapToDTO(refundPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAGA: Errore durante rimborso payment {PaymentId}", refundPaymentDto.PaymentId);
                throw;
            }
        }

        public async Task<PaymentDTO> CancelPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            if (paymentId <= 0)
                throw new ArgumentException("PaymentId è richiesto e deve essere maggiore di 0", nameof(paymentId));

            try
            {
                _logger.LogInformation("SAGA: Avvio cancellazione payment {PaymentId}", paymentId);
                
                // STEP 1: Cancella il pagamento internamente
                var cancelledPayment = await _paymentRepository.CancelPaymentAsync(paymentId, cancellationToken);

                if (cancelledPayment == null)
                {
                    _logger.LogWarning("SAGA: Cancellazione fallita per payment {PaymentId}", paymentId);
                    return null;
                }

                await _paymentRepository.SaveChanges(cancellationToken);
                
                _logger.LogInformation("SAGA: Pagamento cancellato {PaymentId}, ordine {OrderId}", 
                    cancelledPayment.Id, cancelledPayment.OrderId);
                
                // STEP 2: Rilascia tutte le prenotazioni stock (COMPENSAZIONE)
                var stockReleased = await _stockHttp.CancelAllStockReservationsForOrderAsync(cancelledPayment.OrderId, cancellationToken);
                
                if (stockReleased)
                {
                    _logger.LogInformation("SAGA: Stock rilasciato per ordine {OrderId} dopo cancellazione pagamento", cancelledPayment.OrderId);
                }
                else
                {
                    _logger.LogWarning("SAGA: Cancellazione pagamento OK ma errore rilascio stock per ordine {OrderId}", cancelledPayment.OrderId);
                }
                
                // STEP 3: Aggiorna stato ordine a Cancelled
                await _orderHttp.UpdateOrderAsync(cancelledPayment.OrderId, new ShopSaga.OrderService.Shared.UpdateOrderDTO
                {
                    Status = "Cancelled"
                }, cancellationToken);
                
                return MapToDTO(cancelledPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAGA: Errore durante cancellazione payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> IsPaymentProcessedAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _paymentRepository.IsPaymentProcessedAsync(orderId, cancellationToken);
        }

        // Private helper methods
        private PaymentDTO MapToDTO(Payment payment)
        {
            return new PaymentDTO
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Status = payment.Status,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };
        }

        private void ValidateCreatePaymentDto(CreatePaymentDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            
            if (dto.OrderId == 0)
                throw new ArgumentException("OrderId è richiesto", nameof(dto.OrderId));
            
            if (dto.Amount <= 0)
                throw new ArgumentException("L'importo deve essere maggiore di 0", nameof(dto.Amount));
            
            if (string.IsNullOrEmpty(dto.PaymentMethod))
                throw new ArgumentException("Il metodo di pagamento è richiesto", nameof(dto.PaymentMethod));
        }

        private void ValidateRefundPaymentDto(RefundPaymentDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            
            if (dto.PaymentId <= 0)
                throw new ArgumentException("PaymentId è richiesto", nameof(dto.PaymentId));
            
            if (dto.Amount <= 0)
                throw new ArgumentException("L'importo del rimborso deve essere maggiore di 0", nameof(dto.Amount));
                
            if (string.IsNullOrEmpty(dto.Reason))
                throw new ArgumentException("La motivazione è richiesta per il rimborso", nameof(dto.Reason));
        }
    }
}
