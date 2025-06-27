using ShopSaga.PaymentService.Repository.Model;
using ShopSaga.PaymentService.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.PaymentService.Business.Abstraction
{
    public interface IPaymentBusiness
    {
        Task<PaymentDTO> GetPaymentAsync(int id, CancellationToken cancellationToken = default);
        Task<PaymentDTO> CreatePaymentAsync(CreatePaymentDTO createPaymentDto, CancellationToken cancellationToken = default);
        Task<bool> DeletePaymentAsync(int id, CancellationToken cancellationToken = default);
        Task<PaymentDTO> GetPaymentByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PaymentDTO>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<IEnumerable<PaymentDTO>> GetAllPaymentsAsync(CancellationToken cancellationToken = default);
        Task<PaymentDTO> ProcessPaymentAsync(int orderId, CancellationToken cancellationToken = default);
        Task<PaymentDTO> RefundPaymentAsync(RefundPaymentDTO refundPaymentDto, CancellationToken cancellationToken = default);
        Task<PaymentDTO> CancelPaymentAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<bool> IsPaymentProcessedAsync(int orderId, CancellationToken cancellationToken = default);
    }
}