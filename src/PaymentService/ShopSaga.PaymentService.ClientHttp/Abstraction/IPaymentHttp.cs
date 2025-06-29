using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.PaymentService.Shared;

namespace ShopSaga.PaymentService.ClientHttp.Abstraction
{
    public interface IPaymentHttp
    {
        
        Task<PaymentDTO> GetPaymentByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> IsPaymentProcessedAsync(int orderId, CancellationToken cancellationToken = default);
        Task<PaymentDTO> GetPaymentAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PaymentDTO>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<bool> HasPendingPaymentAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> IsOrderFullyPaidAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
