using System.ComponentModel.DataAnnotations;

namespace ShopSaga.PaymentService.Shared
{
    public class RefundPaymentDTO
    {
        [Required]
        public int PaymentId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "L'importo del rimborso deve essere maggiore di 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(255)]
        public string Reason { get; set; }
    }
}
