using ShopSaga.PaymentService.Repository.Abstraction;
using ShopSaga.PaymentService.Repository.Model;
using ShopSaga.PaymentService.Shared;
using ShopSaga.PaymentService.Business.Abstraction;
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

        public PaymentBusiness(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
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
            ValidateCreatePaymentDto(createPaymentDto);

            var payment = new Payment
            {
                OrderId = createPaymentDto.OrderId,
                Amount = createPaymentDto.Amount,
                PaymentMethod = createPaymentDto.PaymentMethod,
                Status = "Pending"
            };

            var createdPayment = await _paymentRepository.CreatePaymentAsync(payment, cancellationToken);
            await _paymentRepository.SaveChanges(cancellationToken);

            return MapToDTO(createdPayment);
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

            var payment = await _paymentRepository.ProcessPaymentAsync(
                orderId,
                cancellationToken);

            await _paymentRepository.SaveChanges(cancellationToken);
            return MapToDTO(payment);
        }

        public async Task<PaymentDTO> RefundPaymentAsync(RefundPaymentDTO refundPaymentDto, CancellationToken cancellationToken = default)
        {
            ValidateRefundPaymentDto(refundPaymentDto);

            var refundPayment = await _paymentRepository.RefundPaymentAsync(
                refundPaymentDto.PaymentId,
                refundPaymentDto.Amount,
                refundPaymentDto.Reason,
                cancellationToken);

            await _paymentRepository.SaveChanges(cancellationToken);
            return MapToDTO(refundPayment);
        }

        public async Task<PaymentDTO> CancelPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            if (paymentId <= 0)
                throw new ArgumentException("PaymentId è richiesto e deve essere maggiore di 0", nameof(paymentId));

            var cancelledPayment = await _paymentRepository.CancelPaymentAsync(
                paymentId,
                cancellationToken);

            if (cancelledPayment == null)
                return null;

            await _paymentRepository.SaveChanges(cancellationToken);
            return MapToDTO(cancelledPayment);
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
