using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Repository.Model;
using ShopSaga.OrderService.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Business
{
    public class SagaOrchestrator : ISagaOrchestrator
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISagaStateRepository _sagaStateRepository;
        
        // Inietterei qui i client per i servizi di pagamento e stock
        // private readonly IPaymentServiceClient _paymentClient;
        // private readonly IStockServiceClient _stockClient;
        
        public SagaOrchestrator(
            IOrderRepository orderRepository,
            ISagaStateRepository sagaStateRepository
            // IPaymentServiceClient paymentClient,
            // IStockServiceClient stockClient
            )
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _sagaStateRepository = sagaStateRepository ?? throw new ArgumentNullException(nameof(sagaStateRepository));
            // _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
            // _stockClient = stockClient ?? throw new ArgumentNullException(nameof(stockClient));
        }

    public async Task<SagaStateDTO> GetSagaStateAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var sagaState = await _sagaStateRepository.GetByOrderIdAsync(orderId, cancellationToken);
        // Conversione diretta senza mapper
        return new SagaStateDTO
        {
            OrderId = sagaState?.OrderId ?? 0,
            Status = sagaState?.Status ?? string.Empty,
            PaymentStatus = sagaState?.PaymentStatus ?? string.Empty,
            StockStatus = sagaState?.StockStatus ?? string.Empty,
            CreatedAt = sagaState?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = sagaState?.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<SagaStateDTO> StartSagaAsync(int orderId, CancellationToken cancellationToken = default)
    {
        // Verifica che l'ordine esista
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"Ordine con ID {orderId} non trovato", nameof(orderId));
            
        // Verifica che lo stato dell'ordine sia quello iniziale
        if (order.Status != "Created")
            throw new InvalidOperationException($"Non è possibile avviare la saga per un ordine nello stato {order.Status}");
            
        // Aggiorna lo stato dell'ordine
        order.Status = "Processing";
        await _orderRepository.UpdateOrderAsync(order, cancellationToken);
        
        // Crea un nuovo stato saga
        var sagaState = new SagaState
        {
            OrderId = orderId,
            Status = "Started",
            PaymentStatus = "Pending",
            StockStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await _sagaStateRepository.CreateAsync(sagaState, cancellationToken);
        
        // Salva le modifiche nel database
        await _sagaStateRepository.SaveChanges(cancellationToken);
        
        // Qui dovresti avviare in modo asincrono le chiamate ai servizi di pagamento e stock
        // await _paymentClient.RequestPaymentAsync(order, cancellationToken);
        
        // Conversione diretta senza mapper
        return new SagaStateDTO
        {
            OrderId = sagaState.OrderId,
            Status = sagaState.Status,
            PaymentStatus = sagaState.PaymentStatus,
            StockStatus = sagaState.StockStatus,
            CreatedAt = sagaState.CreatedAt,
            UpdatedAt = sagaState.UpdatedAt
        };
    }

    public async Task<SagaStateDTO> UpdatePaymentStatusAsync(int orderId, string paymentStatus, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(paymentStatus))
            throw new ArgumentException("Lo stato del pagamento non può essere vuoto", nameof(paymentStatus));
            
        var sagaState = await _sagaStateRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (sagaState == null)
            throw new InvalidOperationException($"Saga non trovata per l'ordine con ID {orderId}");
            
        sagaState.PaymentStatus = paymentStatus;
        
        // Aggiorna lo stato complessivo della saga in base agli stati dei singoli componenti
        await UpdateSagaStatusAsync(sagaState, cancellationToken);
        
        // Conversione diretta senza mapper
        return new SagaStateDTO
        {
            OrderId = sagaState.OrderId,
            Status = sagaState.Status,
            PaymentStatus = sagaState.PaymentStatus,
            StockStatus = sagaState.StockStatus,
            CreatedAt = sagaState.CreatedAt,
            UpdatedAt = sagaState.UpdatedAt
        };
    }

    public async Task<SagaStateDTO> UpdateStockStatusAsync(int orderId, string stockStatus, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(stockStatus))
            throw new ArgumentException("Lo stato del magazzino non può essere vuoto", nameof(stockStatus));
            
        var sagaState = await _sagaStateRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (sagaState == null)
            throw new InvalidOperationException($"Saga non trovata per l'ordine con ID {orderId}");
            
        sagaState.StockStatus = stockStatus;
        
        // Aggiorna lo stato complessivo della saga in base agli stati dei singoli componenti
        await UpdateSagaStatusAsync(sagaState, cancellationToken);
        
        // Conversione diretta senza mapper
        return new SagaStateDTO
        {
            OrderId = sagaState.OrderId,
            Status = sagaState.Status,
            PaymentStatus = sagaState.PaymentStatus,
            StockStatus = sagaState.StockStatus,
            CreatedAt = sagaState.CreatedAt,
            UpdatedAt = sagaState.UpdatedAt
        };
    }
        
        private async Task UpdateSagaStatusAsync(SagaState sagaState, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetOrderByIdAsync(sagaState.OrderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException($"Ordine con ID {sagaState.OrderId} non trovato");
                
            // Logica per aggiornare lo stato della saga in base agli stati dei componenti
            if (sagaState.PaymentStatus == "Failed" || sagaState.StockStatus == "Failed")
            {
                sagaState.Status = "Failed";
                order.Status = "Failed";
                
                // Qui dovresti implementare la logica di compensazione
                // Se il pagamento è completato ma lo stock fallisce, dovresti rimborsare il pagamento
                // await CompensateAsync(sagaState, cancellationToken);
            }
            else if (sagaState.PaymentStatus == "Completed" && sagaState.StockStatus == "Pending")
            {
                sagaState.Status = "PaymentReceived";
                order.Status = "PaymentReceived";
                
                // Ora che il pagamento è completato, possiamo procedere con la verifica dello stock
                // await _stockClient.ReserveStockAsync(order, cancellationToken);
            }
            else if (sagaState.PaymentStatus == "Completed" && sagaState.StockStatus == "Confirmed")
            {
                sagaState.Status = "Completed";
                order.Status = "StockConfirmed";
                
                // La saga è completata con successo, possiamo procedere con la spedizione
                // await PrepareForShippingAsync(order, cancellationToken);
            }
            
            // Aggiorna lo stato della saga nel database
            await _sagaStateRepository.UpdateAsync(sagaState, cancellationToken);
            
            // Aggiorna lo stato dell'ordine nel database
            await _orderRepository.UpdateOrderAsync(order, cancellationToken);
            
            // Salva le modifiche nel database
            await _sagaStateRepository.SaveChanges(cancellationToken);
            await _orderRepository.SaveChanges(cancellationToken);
        }
    }
}
