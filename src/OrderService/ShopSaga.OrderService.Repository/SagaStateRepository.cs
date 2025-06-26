using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Repository.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.Repository
{
    public class SagaStateRepository : ISagaStateRepository
    {
        private readonly OrderDbContext _context;

        public SagaStateRepository(OrderDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }
        
        public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SagaState> CreateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
        {
            if (sagaState == null)
                throw new ArgumentNullException(nameof(sagaState));
            
            sagaState.CreatedAt = DateTime.UtcNow;
            sagaState.UpdatedAt = DateTime.UtcNow;
            
            _context.SagaStates.Add(sagaState);
            
            return sagaState;
        }

        public async Task<SagaState> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.SagaStates
                .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
        }

        public async Task<SagaState> UpdateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
        {
            if (sagaState == null)
                throw new ArgumentNullException(nameof(sagaState));
                
            sagaState.UpdatedAt = DateTime.UtcNow;
            _context.Entry(sagaState).State = EntityState.Modified;
            
            return sagaState;
        }
    }
}
