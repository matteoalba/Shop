using Microsoft.EntityFrameworkCore;
using ShopSaga.PaymentService.Repository.Abstraction;
using ShopSaga.PaymentService.Repository.Model;
using ShopSaga.PaymentService.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.PaymentService.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        // CRUD
        public async Task<Payment> GetPaymentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            
            await _context.Payments.AddAsync(payment, cancellationToken);
            return payment;
        }

        public async Task<bool> DeletePaymentAsync(int id, CancellationToken cancellationToken = default)
        {
            var payment = await _context.Payments.FindAsync(new object[] { id }, cancellationToken);
            if (payment == null) 
                return false;

            _context.Payments.Remove(payment);
            return true;
        }

        // Utility methods
        public async Task<Payment> GetPaymentByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Where(p => p.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Payments.ToListAsync(cancellationToken);
        }

        // Business Logic Methods
        public async Task<Payment> ProcessPaymentAsync(int orderId, CancellationToken cancellationToken = default)
        {
            // Trova il pagamento esistente che deve essere in stato "Pending"
            var payment = await GetPaymentByOrderIdAsync(orderId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Nessun pagamento trovato per l'ordine {orderId}");
            }

            if (payment.Status != "Pending")
            {
                throw new InvalidOperationException($"Il pagamento per l'ordine {orderId} non è in stato 'Pending'. Stato attuale: {payment.Status}");
            }

            // Simula l'approvazione bancaria (sempre true per ora)
            bool bankApproval = SimulateBankPayment();

            if (bankApproval)
            {
                payment.Status = "Completed";
                payment.TransactionId = Guid.NewGuid().ToString();
            }
            else
            {
                payment.Status = "Failed";
                payment.TransactionId = $"FAILED_{Guid.NewGuid()}";
            }

            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);
            
            return payment;
        }

        private bool SimulateBankPayment()
        {
            // Simula sempre successo (true)
            // In futuro potremmo aggiungere logica per simulare fallimenti casuali
            return true;
        }

        public async Task<Payment> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason, CancellationToken cancellationToken = default)
        {
            var payment = await GetPaymentByIdAsync(paymentId, cancellationToken);
            if (payment == null)
                return null;

            // Regola 1: Il pagamento deve essere completato
            if (payment.Status != "Completed")
            {
                throw new InvalidOperationException("È possibile rimborsare solo i pagamenti completati");
            }

            // Regola 2: Non devono essere passati più di 30 giorni
            var daysSincePurchase = (DateTime.UtcNow - payment.CreatedAt).Days;
            if (daysSincePurchase > 30)
            {
                throw new InvalidOperationException("Non è possibile richiedere un rimborso dopo 30 giorni dall'acquisto");
            }

            // Regola 3: Verificare se è già stato refundato
            var existingRefund = await _context.PaymentRefunds
                .AnyAsync(pr => pr.PaymentId == paymentId, cancellationToken);
            if (existingRefund)
            {
                throw new InvalidOperationException("Questo pagamento è già stato rimborsato");
            }

            // Regola 4: L'importo deve essere esattamente uguale all'importo pagato
            if (refundAmount != payment.Amount)
            {
                throw new InvalidOperationException($"L'importo del rimborso deve essere esattamente {payment.Amount:C}");
            }

            // Creare il record di refund nella tabella PaymentRefund
            var paymentRefund = new PaymentRefund
            {
                PaymentId = paymentId,
                Amount = refundAmount,
                Reason = reason,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.PaymentRefunds.AddAsync(paymentRefund, cancellationToken);

            // Aggiornare lo stato del pagamento
            payment.Status = "Refunded";
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);

            return payment;
        }

        public async Task<Payment> CancelPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await GetPaymentByIdAsync(paymentId, cancellationToken);
            if (payment == null)
                return null;

            if (payment.Status != "Pending")
            {
                throw new InvalidOperationException("Non è possibile annullare questo pagamento.");
            }

            payment.Status = "Cancelled";
            payment.UpdatedAt = DateTime.UtcNow;

            _context.Payments.Update(payment);
            return payment;
        }

        public async Task<bool> IsPaymentProcessedAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var payment = await GetPaymentByOrderIdAsync(orderId, cancellationToken);
            return payment != null && (payment.Status == "Completed" || payment.Status == "Refunded");
        }

        public async Task<bool> CanRefundPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await GetPaymentByIdAsync(paymentId, cancellationToken);
            if (payment == null)
                return false;

            // Il refund è valido solo se il pagamento è completato e se non è già stato completamente rimborsato
            if (payment.Status != "Completed")
                return false;

            var totalRefunded = await GetTotalRefundedAmountAsync(paymentId, cancellationToken);
            return totalRefunded < payment.Amount;
        }

        public async Task<decimal> GetTotalRefundedAmountAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            var originalPayment = await GetPaymentByIdAsync(paymentId, cancellationToken);
            if (originalPayment == null)
                return 0;

            // Sum all negative amounts (refunds) for this order
            var refunds = await _context.Payments
                .Where(p => p.OrderId == originalPayment.OrderId && p.Amount < 0)
                .SumAsync(p => Math.Abs(p.Amount), cancellationToken);

            return refunds;
        }

        public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
