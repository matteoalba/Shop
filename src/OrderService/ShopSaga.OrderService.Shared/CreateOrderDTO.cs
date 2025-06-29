using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopSaga.OrderService.Shared
{
    public class CreateOrderDTO
    {
        [Required]
        public List<CreateOrderItemDTO> OrderItems { get; set; }
    }

    public class CreateOrderItemDTO
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantità deve essere maggiore di zero")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Il prezzo unitario deve essere maggiore di zero")]
        public decimal UnitPrice { get; set; }
    }
}
