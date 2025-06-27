using ShopSaga.PaymentService.Repository.Model;
using ShopSaga.PaymentService.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.PaymentService.Repository.Abstraction
{
    public interface IPaymentRepository
    {
        // CRUD
        Task<Payment> GetPaymentByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
        Task<Payment> ProcessPaymentAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> DeletePaymentAsync(int id, CancellationToken cancellationToken = default);
        // Utility
        Task<Payment> GetPaymentByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetAllPaymentsAsync(CancellationToken cancellationToken = default);
        Task<Payment> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason, CancellationToken cancellationToken = default);
        Task<Payment> CancelPaymentAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<bool> IsPaymentProcessedAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> CanRefundPaymentAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalRefundedAmountAsync(int paymentId, CancellationToken cancellationToken = default);
        // Salvataggio
        Task<int> SaveChanges(CancellationToken cancellationToken = default);
    }
}