using ShopSaga.OrderService.Repository.Model;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Repository.Abstraction
{
    public interface ISagaStateRepository
    {
        Task<SagaState> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<SagaState> CreateAsync(SagaState sagaState, CancellationToken cancellationToken = default);
        Task<SagaState> UpdateAsync(SagaState sagaState, CancellationToken cancellationToken = default);
        Task<int> SaveChanges(CancellationToken cancellationToken = default);
    }
}
