using System;
using System.ComponentModel.DataAnnotations;

namespace ShopSaga.PaymentService.Shared
{
    public class CreatePaymentDTO
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "L'importo deve essere maggiore di 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; }
    }
}
