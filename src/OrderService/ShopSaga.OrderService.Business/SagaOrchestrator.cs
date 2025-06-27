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
            return new SagaStateDTO();   
        }

        public async Task<SagaStateDTO> StartSagaAsync(int orderId, CancellationToken cancellationToken = default)
        {
             return new SagaStateDTO();  
        }

        public async Task<SagaStateDTO> UpdatePaymentStatusAsync(int orderId, string paymentStatus, CancellationToken cancellationToken = default)
        {
             return new SagaStateDTO();  
        }

        public async Task<SagaStateDTO> UpdateStockStatusAsync(int orderId, string stockStatus, CancellationToken cancellationToken = default)
        {
             return new SagaStateDTO();  
        }
    }
}
