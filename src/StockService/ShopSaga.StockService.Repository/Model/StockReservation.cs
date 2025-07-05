using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSaga.StockService.Repository.Model
{
    public class StockReservation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
