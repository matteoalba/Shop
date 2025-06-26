using ShopSaga.OrderService.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Business.Abstraction
{
    public interface ISagaOrchestrator
    {
        Task<SagaStateDTO> StartSagaAsync(int orderId, CancellationToken cancellationToken = default);
        
        Task<SagaStateDTO> UpdatePaymentStatusAsync(int orderId, string paymentStatus, CancellationToken cancellationToken = default);

        Task<SagaStateDTO> UpdateStockStatusAsync(int orderId, string stockStatus, CancellationToken cancellationToken = default);
        
        Task<SagaStateDTO> GetSagaStateAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
